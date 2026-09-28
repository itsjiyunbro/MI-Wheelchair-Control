
# ============================================================
# EEG Motor Imagery - FINAL 103 SUBJECT PREPROCESSING PIPELINE
# ============================================================
# Dataset:
#   PhysioNet EEG Motor Movement/Imagery Dataset v1.0.0
#
# Final requirements implemented:
#   - Subjects: 109 total -> exclude S088, S089, S092, S100, S104, S106
#               -> 103 subjects
#   - Runs: R04, R08, R12 only
#   - T0 = Rest
#   - T1 = Left hand MI
#   - T2 = Right hand MI
#   - Channels:
#       FC3, FCz, FC4,
#       C3, Cz, C4,
#       CP3, CPz, CP4
#   - Filtering:
#       8~30 Hz, 2nd-order Butterworth, causal (phase='forward')
#       Run 전체를 먼저 filtering -> window 분할
#   - No notch filter
#   - No cosine/Hann/Tukey tapering
#   - No baseline correction
#   - Window:
#       0.50~2.50
#       0.75~2.75
#       1.00~3.00
#       1.25~3.25
#       1.50~3.50
#       1.75~3.75
#       2.00~4.00
#       => 7 windows/trial, each (9, 320)
#   - Artifact:
#       NaN/Inf
#       flat channel
#       |amplitude| > 100 uV
#       T0/T1/T2 same rules
#       No ICA
#       Do NOT force artifact rate below 5%; report actual removals
#   - Split:
#       subject-level split BEFORE preprocessing/window collection
#       70% train / 15% validation / 15% test
#       random seed = 42
#       same subject's T0/T1/T2 stay in the same split
#   - Standardization:
#       mean/std computed ONLY from TRAIN T1/T2 windows
#       same mean/std applied to:
#           train T1/T2
#           val T1/T2
#           test T1/T2
#           rest train/val/test
#   - Preserve both:
#       standardized arrays
#       unstandardized, filtered arrays
#   - Save metadata and QC reports
#
# IMPORTANT:
#   This script must be run on a machine containing the raw EDF files.
#   The assistant environment does not contain the 103-subject EDF data.
# ============================================================

from pathlib import Path
import json
import sys
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import mne


# ============================================================
# 1. CONFIGURATION
# ============================================================

SUBJECT_MIN = 1
SUBJECT_MAX = 109

EXCLUDED_SUBJECTS = {
    "S088", "S089", "S092",
    "S100", "S104", "S106"
}

TARGET_RUNS = ["04", "08", "12"]

# Change only this list for the later 3-channel comparison:
TARGET_CHANNELS = [
    "FC3", "FCz", "FC4",
    "C3", "Cz", "C4",
    "CP3", "CPz", "CP4"
]
# Example:
# TARGET_CHANNELS = ["C3", "Cz", "C4"]

FS = 160

LOW_CUT = 8.0
HIGH_CUT = 30.0
FILTER_ORDER = 2

WINDOW_SEC = 2.0
STEP_SEC = 0.25
ONSET_START_SEC = 0.5
ONSET_END_SEC = 4.0

WINDOW_SIZE = int(WINDOW_SEC * FS)       # 320
STEP_SIZE = int(STEP_SEC * FS)           # 40
ONSET_START = int(ONSET_START_SEC * FS)  # 80
ONSET_END = int(ONSET_END_SEC * FS)      # 640

NUM_WINDOWS = int(
    (ONSET_END - ONSET_START - WINDOW_SIZE) / STEP_SIZE
) + 1

EXPECTED_WINDOW_STARTS = [
    0.50, 0.75, 1.00, 1.25, 1.50, 1.75, 2.00
]

VOLTAGE_THRESHOLD = 100e-6  # +/-100 uV

# Subject-level split
RANDOM_SEED = 42
TRAIN_RATIO = 0.70
VAL_RATIO = 0.15
TEST_RATIO = 0.15

# Output
OUTPUT_DIR = Path("processed_EEG_103subjects_final")

# Possible raw dataset layouts
DATASET_ROOT_CANDIDATES = [
    Path("eeg-motor-movementimagery-dataset-1.0.0/files"),
    Path("eeg-motor-movementimagery-dataset-1.0.0"),
    Path("."),
]


# Labels
EVENT_MAPPING = {
    "T0": -1,   # Rest
    "T1": 0,    # Left hand MI
    "T2": 1     # Right hand MI
}

LABEL_NAME = {
    -1: "Rest",
    0: "Left",
    1: "Right"
}


# ============================================================
# 2. HELPER FUNCTIONS
# ============================================================

def find_dataset_root():
    """Find the folder that contains S001/S001R04.edf etc."""
    for root in DATASET_ROOT_CANDIDATES:
        for subject in ["S001", "S002"]:
            candidate = root / subject / f"{subject}R04.edf"
            if candidate.exists():
                return root

    # Also support an extracted folder where .edf files are nested differently.
    for root in DATASET_ROOT_CANDIDATES:
        if root.exists():
            matches = list(root.rglob("S001R04.edf"))
            if matches:
                return matches[0].parent

    return None


def find_edf(dataset_root: Path, subject_id: str, run_id: str):
    """Find one EDF file."""
    candidates = [
        dataset_root / subject_id / f"{subject_id}R{run_id}.edf",
        dataset_root / f"{subject_id}R{run_id}.edf",
        dataset_root / subject_id / f"{subject_id}R{run_id}.EDF",
        dataset_root / f"{subject_id}R{run_id}.EDF",
    ]

    for p in candidates:
        if p.exists():
            return p

    # Fallback recursive search within dataset root
    pattern = f"{subject_id}R{run_id}.edf"
    matches = list(dataset_root.rglob(pattern))
    if matches:
        return matches[0]

    return None


def get_expected_subjects():
    subjects = [
        f"S{i:03d}"
        for i in range(SUBJECT_MIN, SUBJECT_MAX + 1)
    ]

    subjects = [
        s for s in subjects
        if s not in EXCLUDED_SUBJECTS
    ]

    if len(subjects) != 103:
        raise RuntimeError(
            f"Expected 103 subjects after exclusion, got {len(subjects)}"
        )

    return subjects


def make_subject_split(subjects):
    """
    Deterministic 70/15/15 subject-level split.
    103 subjects -> 72 train / 15 val / 16 test.
    """
    subjects = list(subjects)

    rng = np.random.default_rng(RANDOM_SEED)
    shuffled = np.array(subjects, dtype=object)
    rng.shuffle(shuffled)

    n_total = len(shuffled)
    n_train = int(np.floor(n_total * TRAIN_RATIO))
    n_val = int(np.floor(n_total * VAL_RATIO))
    n_test = n_total - n_train - n_val

    train_subjects = sorted(shuffled[:n_train].tolist())
    val_subjects = sorted(
        shuffled[n_train:n_train + n_val].tolist()
    )
    test_subjects = sorted(
        shuffled[n_train + n_val:].tolist()
    )

    if len(train_subjects) + len(val_subjects) + len(test_subjects) != n_total:
        raise RuntimeError("Subject split count mismatch.")

    if (
        set(train_subjects) & set(val_subjects)
        or set(train_subjects) & set(test_subjects)
        or set(val_subjects) & set(test_subjects)
    ):
        raise RuntimeError("Subject leakage detected between splits.")

    return {
        "train": train_subjects,
        "val": val_subjects,
        "test": test_subjects
    }


def check_artifact(window_data):
    """
    window_data: (channels, samples)

    Returns:
        is_artifact: bool
        reasons: list[str]
    """
    reasons = []

    if not np.isfinite(window_data).all():
        reasons.append("nan_or_inf")

    # A channel is considered flat if all 320 samples are identical.
    channel_ptp = np.ptp(window_data, axis=1)
    if np.any(channel_ptp == 0):
        reasons.append("flat_channel")

    # Explicit +/-100 uV amplitude criterion.
    if np.max(np.abs(window_data)) > VOLTAGE_THRESHOLD:
        reasons.append("amplitude_over_100uV")

    return len(reasons) > 0, reasons


def compute_psd_summary(raw_before, raw_after, subject_id, run_id):
    """
    Compare whole-run PSD before and after filtering.
    Returns numeric QC summary, not a model feature.
    """
    from mne.time_frequency import psd_array_welch

    before_data = raw_before.get_data()
    after_data = raw_after.get_data()

    psd_before, freqs = psd_array_welch(
        before_data,
        sfreq=FS,
        fmin=1.0,
        fmax=60.0,
        n_fft=1024,
        n_overlap=512,
        average="mean",
        verbose=False
    )

    psd_after, _ = psd_array_welch(
        after_data,
        sfreq=FS,
        fmin=1.0,
        fmax=60.0,
        n_fft=1024,
        n_overlap=512,
        average="mean",
        verbose=False
    )

    mean_before = psd_before.mean(axis=0)
    mean_after = psd_after.mean(axis=0)

    bands = {
        "1-8Hz": (1, 8),
        "8-30Hz": (8, 30),
        "30-40Hz": (30, 40),
        "40-50Hz": (40, 50),
        "50-60Hz": (50, 60),
    }

    rows = []

    for band_name, (f_low, f_high) in bands.items():
        mask = (freqs >= f_low) & (freqs < f_high)

        if np.sum(mask) < 2:
            before_power = np.nan
            after_power = np.nan
            ratio_db = np.nan
        else:
            before_power = np.trapezoid(
                mean_before[mask], freqs[mask]
            ) if hasattr(np, "trapezoid") else np.trapz(
                mean_before[mask], freqs[mask]
            )

            after_power = np.trapezoid(
                mean_after[mask], freqs[mask]
            ) if hasattr(np, "trapezoid") else np.trapz(
                mean_after[mask], freqs[mask]
            )

            ratio_db = 10 * np.log10(
                (after_power + 1e-30)
                / (before_power + 1e-30)
            )

        rows.append({
            "Subject": subject_id,
            "Run": run_id,
            "Band": band_name,
            "Before_Power": before_power,
            "After_Power": after_power,
            "After_vs_Before_dB": ratio_db
        })

    return pd.DataFrame(rows), freqs, mean_before, mean_after


def save_psd_plot(
    freqs,
    mean_before,
    mean_after,
    subject_id,
    run_id
):
    plt.figure(figsize=(10, 5))

    plt.semilogy(
        freqs,
        mean_before,
        label="Before filtering"
    )
    plt.semilogy(
        freqs,
        mean_after,
        label="After 8-30 Hz causal Butterworth"
    )

    plt.axvline(LOW_CUT, linestyle="--", label="8 Hz")
    plt.axvline(HIGH_CUT, linestyle="--", label="30 Hz")

    plt.xlabel("Frequency (Hz)")
    plt.ylabel("PSD (V²/Hz)")
    plt.title(
        f"{subject_id} R{run_id}: PSD Before vs After"
    )
    plt.xlim(1, 60)
    plt.grid(True, alpha=0.3)
    plt.legend()
    plt.tight_layout()

    output = (
        OUTPUT_DIR
        / "qc_psd_plots"
        / f"{subject_id}R{run_id}_PSD_before_after.png"
    )
    output.parent.mkdir(
        parents=True,
        exist_ok=True
    )
    plt.savefig(output, dpi=130)
    plt.close()


def process_run(file_path: Path, subject_id: str, run_id: str):
    """
    Run 전체:
      EDF
      -> standardize
      -> pick 9 channels
      -> whole-run causal BPF
      -> event extraction
      -> 0~4 sec epoch
      -> 0.5~4.0 sec sliding windows
      -> artifact QC
    """

    raw = mne.io.read_raw_edf(
        str(file_path),
        preload=True,
        verbose=False
    )

    # Standardize PhysioNet EEGBCI channel names.
    mne.datasets.eegbci.standardize(raw)

    # Keep only requested channels.
    raw.pick(TARGET_CHANNELS)

    if raw.info["sfreq"] != FS:
        raise ValueError(
            f"{subject_id} R{run_id}: "
            f"expected sfreq={FS}, "
            f"got {raw.info['sfreq']}"
        )

    if raw.ch_names != TARGET_CHANNELS:
        # The order should be the requested order.
        missing = set(TARGET_CHANNELS) - set(raw.ch_names)
        if missing:
            raise ValueError(
                f"{subject_id} R{run_id}: missing channels {missing}"
            )

    # Keep the original selected-channel whole-run signal for PSD QC.
    raw_before = raw.copy()

    # --------------------------------------------------------
    # Run 전체 먼저 causal filtering
    # --------------------------------------------------------
    iir_params = {
        "order": FILTER_ORDER,
        "ftype": "butter",
        "output": "sos"
    }

    raw.filter(
        l_freq=LOW_CUT,
        h_freq=HIGH_CUT,
        method="iir",
        iir_params=iir_params,
        phase="forward",
        verbose=False
    )

    raw_after = raw

    # PSD QC
    psd_df, freqs, mean_before, mean_after = compute_psd_summary(
        raw_before,
        raw_after,
        subject_id,
        run_id
    )

    # Save plots for every run.
    # 309 plots is manageable, but may be omitted by setting SAVE_ALL_PSD_PLOTS=False.
    if SAVE_ALL_PSD_PLOTS:
        save_psd_plot(
            freqs,
            mean_before,
            mean_after,
            subject_id,
            run_id
        )

    # --------------------------------------------------------
    # Events
    # --------------------------------------------------------
    events, event_id = mne.events_from_annotations(
        raw_after,
        verbose=False
    )

    reverse_event_id = {
        value: key
        for key, value in event_id.items()
    }

    event_counts = {
        "T0": 0,
        "T1": 0,
        "T2": 0
    }

    for e in events:
        name = reverse_event_id.get(e[2])
        if name in event_counts:
            event_counts[name] += 1

    # --------------------------------------------------------
    # Epoch 0~4 sec
    # --------------------------------------------------------
    epochs = mne.Epochs(
        raw_after,
        events,
        tmin=0.0,
        tmax=4.0,
        baseline=None,
        preload=True,
        reject_by_annotation=False,
        verbose=False
    )

    epoch_data = epochs.get_data()
    epoch_event_codes = epochs.events[:, -1]

    X_list = []
    y_list = []
    meta_list = []
    qc_rows = []

    valid_epoch_indices = set(range(len(epoch_data)))

    for epoch_i, trial_data in enumerate(epoch_data):

        event_code = epoch_event_codes[epoch_i]
        event_name = reverse_event_id.get(event_code)

        if event_name not in EVENT_MAPPING:
            continue

        mapped_label = EVENT_MAPPING[event_name]

        candidate_windows = 0
        kept_windows = 0
        removed_windows = 0

        reason_counts = {
            "nan_or_inf": 0,
            "flat_channel": 0,
            "amplitude_over_100uV": 0
        }

        for w in range(NUM_WINDOWS):

            candidate_windows += 1

            start_idx = ONSET_START + w * STEP_SIZE
            end_idx = start_idx + WINDOW_SIZE

            window_data = trial_data[:, start_idx:end_idx]

            if window_data.shape != (
                len(TARGET_CHANNELS),
                WINDOW_SIZE
            ):
                raise ValueError(
                    f"{subject_id} R{run_id} Trial {epoch_i} "
                    f"Window {w}: shape {window_data.shape}"
                )

            is_artifact, reasons = check_artifact(window_data)

            if is_artifact:
                removed_windows += 1

                for reason in reasons:
                    reason_counts[reason] += 1

                continue

            kept_windows += 1

            start_sec = (
                ONSET_START_SEC
                + w * STEP_SEC
            )
            end_sec = start_sec + WINDOW_SEC

            X_list.append(
                window_data.astype(np.float32)
            )

            y_list.append(mapped_label)

            meta_list.append({
                "Subject": subject_id,
                "Run": run_id,
                "Trial": epoch_i,
                "Window_Idx": w,
                "Label": mapped_label,
                "Label_Name": LABEL_NAME[mapped_label],
                "Start_sec": start_sec,
                "End_sec": end_sec
            })

        qc_rows.append({
            "Subject": subject_id,
            "Run": run_id,
            "Trial": epoch_i,
            "Label": mapped_label,
            "Label_Name": LABEL_NAME[mapped_label],
            "Candidate_Windows": candidate_windows,
            "Kept_Windows": kept_windows,
            "Removed_Windows": removed_windows,
            "Removed_nan_or_inf": reason_counts["nan_or_inf"],
            "Removed_flat_channel": reason_counts["flat_channel"],
            "Removed_amplitude_over_100uV": reason_counts[
                "amplitude_over_100uV"
            ]
        })

    return {
        "X": X_list,
        "y": y_list,
        "metadata": meta_list,
        "qc": pd.DataFrame(qc_rows),
        "psd": psd_df,
        "event_counts": event_counts,
        "n_epochs": len(epoch_data)
    }


def process_subjects(
    subjects,
    split_name,
    dataset_root
):
    """
    Process all requested subjects belonging to one split.
    T0/T1/T2 are collected together first, then separated.
    """

    task_X = []
    task_y = []
    task_meta = []

    rest_X = []
    rest_meta = []

    qc_tables = []
    psd_tables = []

    event_summary_rows = []
    missing_files = []

    total_runs = len(subjects) * len(TARGET_RUNS)

    run_counter = 0

    for subject_id in subjects:

        print(
            f"\n[{split_name.upper()}] "
            f"{subject_id}"
        )

        for run_id in TARGET_RUNS:

            run_counter += 1

            file_path = find_edf(
                dataset_root,
                subject_id,
                run_id
            )

            if file_path is None:
                missing_files.append(
                    f"{subject_id}R{run_id}.edf"
                )
                continue

            print(
                f"  ({run_counter}/{total_runs}) "
                f"R{run_id}"
            )

            result = process_run(
                file_path,
                subject_id,
                run_id
            )

            qc_tables.append(result["qc"])
            psd_tables.append(result["psd"])

            event_counts = result["event_counts"]

            event_summary_rows.append({
                "Split": split_name,
                "Subject": subject_id,
                "Run": run_id,
                "T0_Count": event_counts["T0"],
                "T1_Count": event_counts["T1"],
                "T2_Count": event_counts["T2"],
                "Total_Valid_Events": (
                    event_counts["T0"]
                    + event_counts["T1"]
                    + event_counts["T2"]
                ),
                "Epochs_0_to_4sec": result["n_epochs"]
            })

            for X, y, meta in zip(
                result["X"],
                result["y"],
                result["metadata"]
            ):
                if y == -1:
                    rest_X.append(X)
                    rest_meta.append(meta)
                else:
                    task_X.append(X)
                    task_y.append(y)
                    task_meta.append(meta)

    if missing_files:
        missing_path = (
            OUTPUT_DIR
            / f"missing_files_{split_name}.txt"
        )
        missing_path.write_text(
            "\n".join(missing_files),
            encoding="utf-8"
        )

        raise FileNotFoundError(
            f"{split_name}: missing {len(missing_files)} EDF files. "
            f"See {missing_path}"
        )

    X_task = np.asarray(
        task_X,
        dtype=np.float32
    )

    y_task = np.asarray(
        task_y,
        dtype=np.int64
    )

    X_rest = np.asarray(
        rest_X,
        dtype=np.float32
    )

    df_task = pd.DataFrame(task_meta)
    df_rest = pd.DataFrame(rest_meta)

    qc_df = (
        pd.concat(
            qc_tables,
            ignore_index=True
        )
        if qc_tables
        else pd.DataFrame()
    )

    psd_df = (
        pd.concat(
            psd_tables,
            ignore_index=True
        )
        if psd_tables
        else pd.DataFrame()
    )

    event_df = pd.DataFrame(event_summary_rows)

    return {
        "X_task": X_task,
        "y_task": y_task,
        "X_rest": X_rest,
        "metadata_task": df_task,
        "metadata_rest": df_rest,
        "qc": qc_df,
        "psd": psd_df,
        "event_summary": event_df
    }


def validate_split_arrays(data, split_name):
    """Check requested tensor shapes and labels."""
    X_task = data["X_task"]
    y_task = data["y_task"]
    X_rest = data["X_rest"]

    expected_task_dim = (
        len(TARGET_CHANNELS),
        WINDOW_SIZE
    )

    if X_task.ndim != 3:
        raise ValueError(
            f"{split_name}: X_task ndim={X_task.ndim}, expected 3"
        )

    if X_task.shape[1:] != expected_task_dim:
        raise ValueError(
            f"{split_name}: X_task shape={X_task.shape}, "
            f"expected (N, {expected_task_dim[0]}, {expected_task_dim[1]})"
        )

    if y_task.ndim != 1:
        raise ValueError(
            f"{split_name}: y_task ndim={y_task.ndim}"
        )

    if len(y_task) != len(X_task):
        raise ValueError(
            f"{split_name}: X/y length mismatch"
        )

    if not set(np.unique(y_task)).issubset({0, 1}):
        raise ValueError(
            f"{split_name}: task label contains values "
            f"other than 0/1"
        )

    if X_rest.ndim != 3:
        raise ValueError(
            f"{split_name}: X_rest ndim={X_rest.ndim}"
        )

    if X_rest.shape[1:] != expected_task_dim:
        raise ValueError(
            f"{split_name}: X_rest shape={X_rest.shape}"
        )


def standardize_arrays(
    train_task,
    val_task,
    test_task,
    rest_train,
    rest_val,
    rest_test
):
    """
    Mean/std are calculated ONLY from TRAIN T1/T2.
    Channel-wise:
      mean/std across all train windows and time samples.
    """
    if len(train_task) == 0:
        raise RuntimeError(
            "Train T1/T2 data are empty; cannot standardize."
        )

    ch_mean = np.mean(
        train_task,
        axis=(0, 2),
        keepdims=True,
        dtype=np.float64
    )

    ch_std = np.std(
        train_task,
        axis=(0, 2),
        keepdims=True,
        dtype=np.float64
    )

    ch_std[ch_std == 0] = 1e-12

    def apply(x):
        return (
            (x.astype(np.float64) - ch_mean)
            / ch_std
        ).astype(np.float32)

    return {
        "X_train": apply(train_task),
        "X_val": apply(val_task),
        "X_test": apply(test_task),
        "X_rest_train": apply(rest_train),
        "X_rest_val": apply(rest_val),
        "X_rest_test": apply(rest_test),
        "train_mean": ch_mean.astype(np.float32),
        "train_std": ch_std.astype(np.float32)
    }


def save_split_arrays(
    standardized,
    raw_filtered,
    output_dir
):
    """Save final standardized arrays and unstandardized filtered arrays."""

    # Final model-ready task arrays
    np.save(
        output_dir / "X_train.npy",
        standardized["X_train"]
    )
    np.save(
        output_dir / "y_train.npy",
        raw_filtered["train"]["y_task"]
    )

    np.save(
        output_dir / "X_val.npy",
        standardized["X_val"]
    )
    np.save(
        output_dir / "y_val.npy",
        raw_filtered["val"]["y_task"]
    )

    np.save(
        output_dir / "X_test.npy",
        standardized["X_test"]
    )
    np.save(
        output_dir / "y_test.npy",
        raw_filtered["test"]["y_task"]
    )

    # Rest arrays
    np.save(
        output_dir / "X_rest_train.npy",
        standardized["X_rest_train"]
    )
    np.save(
        output_dir / "X_rest_val.npy",
        standardized["X_rest_val"]
    )
    np.save(
        output_dir / "X_rest_test.npy",
        standardized["X_rest_test"]
    )

    # Unstandardized, filtered windows for CSP / alternative models
    np.save(
        output_dir / "X_train_unstd.npy",
        raw_filtered["train"]["X_task"]
    )
    np.save(
        output_dir / "X_val_unstd.npy",
        raw_filtered["val"]["X_task"]
    )
    np.save(
        output_dir / "X_test_unstd.npy",
        raw_filtered["test"]["X_task"]
    )

    np.save(
        output_dir / "X_rest_train_unstd.npy",
        raw_filtered["train"]["X_rest"]
    )
    np.save(
        output_dir / "X_rest_val_unstd.npy",
        raw_filtered["val"]["X_rest"]
    )
    np.save(
        output_dir / "X_rest_test_unstd.npy",
        raw_filtered["test"]["X_rest"]
    )

    # Standardization parameters
    np.save(
        output_dir / "train_channel_mean.npy",
        standardized["train_mean"]
    )
    np.save(
        output_dir / "train_channel_std.npy",
        standardized["train_std"]
    )


def save_metadata(raw_filtered, output_dir):
    for split_name in ["train", "val", "test"]:
        raw_filtered[split_name]["metadata_task"].to_csv(
            output_dir / f"metadata_{split_name}.csv",
            index=False,
            encoding="utf-8-sig"
        )

        # Rest metadata is also saved separately so that
        # T0 can be traced independently.
        raw_filtered[split_name]["metadata_rest"].to_csv(
            output_dir / f"metadata_rest_{split_name}.csv",
            index=False,
            encoding="utf-8-sig"
        )


def save_qc_reports(raw_filtered, output_dir):

    all_qc = []

    all_psd = []

    all_events = []

    for split_name in ["train", "val", "test"]:

        qc = raw_filtered[split_name]["qc"].copy()
        qc["Split"] = split_name
        all_qc.append(qc)

        psd = raw_filtered[split_name]["psd"].copy()
        psd["Split"] = split_name
        all_psd.append(psd)

        events = raw_filtered[split_name]["event_summary"].copy()
        all_events.append(events)

        # Split-specific reports
        qc.to_csv(
            output_dir / f"artifact_qc_{split_name}.csv",
            index=False,
            encoding="utf-8-sig"
        )

        psd.to_csv(
            output_dir / f"psd_qc_{split_name}.csv",
            index=False,
            encoding="utf-8-sig"
        )

        events.to_csv(
            output_dir / f"event_summary_{split_name}.csv",
            index=False,
            encoding="utf-8-sig"
        )

    qc_all = pd.concat(
        all_qc,
        ignore_index=True
    )

    psd_all = pd.concat(
        all_psd,
        ignore_index=True
    )

    events_all = pd.concat(
        all_events,
        ignore_index=True
    )

    qc_all.to_csv(
        output_dir / "artifact_qc_all.csv",
        index=False,
        encoding="utf-8-sig"
    )

    psd_all.to_csv(
        output_dir / "psd_qc_all.csv",
        index=False,
        encoding="utf-8-sig"
    )

    events_all.to_csv(
        output_dir / "event_summary_all.csv",
        index=False,
        encoding="utf-8-sig"
    )

    # Summary by split and label
    qc_summary = (
        qc_all
        .groupby(
            ["Split", "Run", "Label", "Label_Name"],
            as_index=False
        )
        .agg(
            Candidate_Windows=("Candidate_Windows", "sum"),
            Kept_Windows=("Kept_Windows", "sum"),
            Removed_Windows=("Removed_Windows", "sum"),
            Removed_nan_or_inf=("Removed_nan_or_inf", "sum"),
            Removed_flat_channel=("Removed_flat_channel", "sum"),
            Removed_amplitude_over_100uV=(
                "Removed_amplitude_over_100uV",
                "sum"
            )
        )
    )

    qc_summary["Removal_Rate_%"] = (
        qc_summary["Removed_Windows"]
        / qc_summary["Candidate_Windows"]
        * 100
    )

    qc_summary.to_csv(
        output_dir / "artifact_qc_summary.csv",
        index=False,
        encoding="utf-8-sig"
    )

    # Overall summary
    total_candidates = int(
        qc_all["Candidate_Windows"].sum()
    )
    total_kept = int(
        qc_all["Kept_Windows"].sum()
    )
    total_removed = int(
        qc_all["Removed_Windows"].sum()
    )

    overall = pd.DataFrame([{
        "Total_Candidate_Windows": total_candidates,
        "Total_Kept_Windows": total_kept,
        "Total_Removed_Windows": total_removed,
        "Total_Removal_Rate_%": (
            total_removed / total_candidates * 100
            if total_candidates > 0 else np.nan
        ),
        "Removed_nan_or_inf": int(
            qc_all["Removed_nan_or_inf"].sum()
        ),
        "Removed_flat_channel": int(
            qc_all["Removed_flat_channel"].sum()
        ),
        "Removed_amplitude_over_100uV": int(
            qc_all[
                "Removed_amplitude_over_100uV"
            ].sum()
        )
    }])

    overall.to_csv(
        output_dir / "artifact_qc_overall.csv",
        index=False,
        encoding="utf-8-sig"
    )


def validate_subject_split(split):
    all_subjects = set(
        split["train"]
        + split["val"]
        + split["test"]
    )

    if len(all_subjects) != 103:
        raise ValueError(
            f"Split union has {len(all_subjects)} subjects, expected 103."
        )

    expected_subjects = set(
        get_expected_subjects()
    )

    if all_subjects != expected_subjects:
        missing = sorted(expected_subjects - all_subjects)
        extra = sorted(all_subjects - expected_subjects)

        raise ValueError(
            f"Subject split mismatch. "
            f"missing={missing}, extra={extra}"
        )


def save_subject_split(split, output_dir):
    rows = []

    for split_name, subjects in split.items():
        for subject_id in subjects:
            rows.append({
                "Subject": subject_id,
                "Split": split_name,
                "Random_Seed": RANDOM_SEED
            })

    df = pd.DataFrame(rows)

    df.to_csv(
        output_dir / "split_subjects.csv",
        index=False,
        encoding="utf-8-sig"
    )

    # Also save JSON for reproducibility.
    with open(
        output_dir / "split_subjects.json",
        "w",
        encoding="utf-8"
    ) as f:
        json.dump(
            split,
            f,
            ensure_ascii=False,
            indent=2
        )


def validate_no_subject_leakage(raw_filtered):
    subjects_by_split = {}

    for split_name in ["train", "val", "test"]:
        task_meta = raw_filtered[split_name]["metadata_task"]
        rest_meta = raw_filtered[split_name]["metadata_rest"]

        task_subjects = set(
            task_meta["Subject"].unique()
        ) if not task_meta.empty else set()

        rest_subjects = set(
            rest_meta["Subject"].unique()
        ) if not rest_meta.empty else set()

        subjects_by_split[split_name] = (
            task_subjects | rest_subjects
        )

    train = subjects_by_split["train"]
    val = subjects_by_split["val"]
    test = subjects_by_split["test"]

    if train & val:
        raise ValueError(
            f"Train/Val subject leakage: {sorted(train & val)}"
        )

    if train & test:
        raise ValueError(
            f"Train/Test subject leakage: {sorted(train & test)}"
        )

    if val & test:
        raise ValueError(
            f"Val/Test subject leakage: {sorted(val & test)}"
        )


def save_config(output_dir, dataset_root, split):
    config = {
        "dataset": "PhysioNet EEG Motor Movement/Imagery Dataset v1.0.0",
        "dataset_root": str(dataset_root.resolve()),
        "subjects_total_original": 109,
        "subjects_excluded": sorted(EXCLUDED_SUBJECTS),
        "subjects_used": 103,
        "runs": TARGET_RUNS,
        "event_mapping": EVENT_MAPPING,
        "channels": TARGET_CHANNELS,
        "sampling_rate_hz": FS,
        "filter": {
            "type": "Butterworth IIR",
            "order": FILTER_ORDER,
            "low_hz": LOW_CUT,
            "high_hz": HIGH_CUT,
            "phase": "forward",
            "causal": True,
            "notch_filter": False
        },
        "tapering": False,
        "baseline_correction": False,
        "window": {
            "onset_start_sec": ONSET_START_SEC,
            "onset_end_sec": ONSET_END_SEC,
            "window_sec": WINDOW_SEC,
            "step_sec": STEP_SEC,
            "window_samples": WINDOW_SIZE,
            "step_samples": STEP_SIZE,
            "num_windows_per_trial": NUM_WINDOWS,
            "expected_window_starts_sec": EXPECTED_WINDOW_STARTS
        },
        "artifact": {
            "ica": False,
            "nan_or_inf": True,
            "flat_channel": True,
            "absolute_amplitude_threshold_uv": 100
        },
        "split": {
            "method": "subject_level",
            "train_ratio": TRAIN_RATIO,
            "val_ratio": VAL_RATIO,
            "test_ratio": TEST_RATIO,
            "random_seed": RANDOM_SEED
        },
        "standardization": {
            "statistics_source": "TRAIN T1/T2 only",
            "axis": "(windows, time) per channel",
            "applied_to": [
                "train T1/T2",
                "val T1/T2",
                "test T1/T2",
                "train T0",
                "val T0",
                "test T0"
            ]
        },
        "output_files": [
            "X_train.npy",
            "y_train.npy",
            "X_val.npy",
            "y_val.npy",
            "X_test.npy",
            "y_test.npy",
            "X_rest_train.npy",
            "X_rest_val.npy",
            "X_rest_test.npy",
            "X_train_unstd.npy",
            "X_val_unstd.npy",
            "X_test_unstd.npy",
            "X_rest_train_unstd.npy",
            "X_rest_val_unstd.npy",
            "X_rest_test_unstd.npy",
            "train_channel_mean.npy",
            "train_channel_std.npy",
            "split_subjects.csv",
            "metadata_train.csv",
            "metadata_val.csv",
            "metadata_test.csv"
        ]
    }

    with open(
        output_dir / "preprocessing_config.json",
        "w",
        encoding="utf-8"
    ) as f:
        json.dump(
            config,
            f,
            ensure_ascii=False,
            indent=2
        )


# ============================================================
# 3. MAIN
# ============================================================

# Set True to make one PSD PNG per processed run.
# For 103 subjects × 3 runs, this creates 309 PNG files.
SAVE_ALL_PSD_PLOTS = True


def main():

    OUTPUT_DIR.mkdir(
        parents=True,
        exist_ok=True
    )

    # --------------------------------------------------------
    # Dataset root
    # --------------------------------------------------------
    dataset_root = find_dataset_root()

    if dataset_root is None:
        print(
            "\n[ERROR] EDF dataset root를 찾지 못했습니다.\n"
            "현재 Python 파일의 작업 폴더 아래에\n"
            "eeg-motor-movementimagery-dataset-1.0.0/files/S001/\n"
            "같은 구조가 있는지 확인하세요."
        )
        sys.exit(1)

    print("=" * 80)
    print("EEG FINAL 103-SUBJECT PREPROCESSING")
    print("=" * 80)
    print(f"Dataset root: {dataset_root.resolve()}")

    # --------------------------------------------------------
    # Subject list
    # --------------------------------------------------------
    subjects = get_expected_subjects()

    print(f"\nOriginal subjects : 109")
    print(
        f"Excluded subjects : "
        f"{', '.join(sorted(EXCLUDED_SUBJECTS))}"
    )
    print(f"Final subjects    : {len(subjects)}")

    # --------------------------------------------------------
    # Subject-level split BEFORE preprocessing/windowing
    # --------------------------------------------------------
    split = make_subject_split(subjects)

    validate_subject_split(split)

    print("\nSubject split:")
    print(
        f"Train: {len(split['train'])} subjects"
    )
    print(
        f"Val  : {len(split['val'])} subjects"
    )
    print(
        f"Test : {len(split['test'])} subjects"
    )

    save_subject_split(
        split,
        OUTPUT_DIR
    )

    # --------------------------------------------------------
    # Verify all required files exist BEFORE expensive work.
    # --------------------------------------------------------
    print("\nChecking all 309 required EDF files...")

    missing = []

    for subject_id in subjects:
        for run_id in TARGET_RUNS:
            if find_edf(
                dataset_root,
                subject_id,
                run_id
            ) is None:
                missing.append(
                    f"{subject_id}R{run_id}.edf"
                )

    if missing:
        missing_path = (
            OUTPUT_DIR
            / "missing_required_files.txt"
        )

        missing_path.write_text(
            "\n".join(missing),
            encoding="utf-8"
        )

        raise FileNotFoundError(
            f"{len(missing)} required EDF files are missing. "
            f"See {missing_path}"
        )

    print("All required EDF files found.")

    # --------------------------------------------------------
    # Process each subject split.
    # The subject assignment was already fixed before this.
    # --------------------------------------------------------
    raw_filtered = {}

    for split_name in ["train", "val", "test"]:

        raw_filtered[split_name] = process_subjects(
            split[split_name],
            split_name,
            dataset_root
        )

        validate_split_arrays(
            raw_filtered[split_name],
            split_name
        )

        print(
            f"\n[{split_name}] "
            f"T1/T2 windows: "
            f"{len(raw_filtered[split_name]['y_task'])}"
        )

        print(
            f"[{split_name}] "
            f"T0 windows: "
            f"{len(raw_filtered[split_name]['X_rest'])}"
        )

        if len(raw_filtered[split_name]["y_task"]) > 0:
            label_counts = pd.Series(
                raw_filtered[split_name]["y_task"]
            ).value_counts().sort_index()

            print(
                f"[{split_name}] "
                f"Left(0)={int(label_counts.get(0, 0))}, "
                f"Right(1)={int(label_counts.get(1, 0))}"
            )

    # --------------------------------------------------------
    # Subject leakage verification
    # --------------------------------------------------------
    validate_no_subject_leakage(
        raw_filtered
    )

    # --------------------------------------------------------
    # Standardization:
    # TRAIN T1/T2 ONLY
    # --------------------------------------------------------
    standardized = standardize_arrays(
        train_task=raw_filtered["train"]["X_task"],
        val_task=raw_filtered["val"]["X_task"],
        test_task=raw_filtered["test"]["X_task"],
        rest_train=raw_filtered["train"]["X_rest"],
        rest_val=raw_filtered["val"]["X_rest"],
        rest_test=raw_filtered["test"]["X_rest"]
    )

    # --------------------------------------------------------
    # Final save
    # --------------------------------------------------------
    save_split_arrays(
        standardized,
        raw_filtered,
        OUTPUT_DIR
    )

    save_metadata(
        raw_filtered,
        OUTPUT_DIR
    )

    save_qc_reports(
        raw_filtered,
        OUTPUT_DIR
    )

    save_config(
        OUTPUT_DIR,
        dataset_root,
        split
    )

    # --------------------------------------------------------
    # Final shape / label / standardization validation
    # --------------------------------------------------------
    final_arrays = {
        "X_train": np.load(
            OUTPUT_DIR / "X_train.npy"
        ),
        "y_train": np.load(
            OUTPUT_DIR / "y_train.npy"
        ),
        "X_val": np.load(
            OUTPUT_DIR / "X_val.npy"
        ),
        "y_val": np.load(
            OUTPUT_DIR / "y_val.npy"
        ),
        "X_test": np.load(
            OUTPUT_DIR / "X_test.npy"
        ),
        "y_test": np.load(
            OUTPUT_DIR / "y_test.npy"
        ),
        "X_rest_train": np.load(
            OUTPUT_DIR / "X_rest_train.npy"
        ),
        "X_rest_val": np.load(
            OUTPUT_DIR / "X_rest_val.npy"
        ),
        "X_rest_test": np.load(
            OUTPUT_DIR / "X_rest_test.npy"
        )
    }

    expected_input_shape = (
        len(TARGET_CHANNELS),
        WINDOW_SIZE
    )

    for name in [
        "X_train",
        "X_val",
        "X_test",
        "X_rest_train",
        "X_rest_val",
        "X_rest_test"
    ]:
        arr = final_arrays[name]

        if arr.ndim != 3:
            raise ValueError(
                f"{name}: ndim={arr.ndim}"
            )

        if arr.shape[1:] != expected_input_shape:
            raise ValueError(
                f"{name}: shape={arr.shape}, "
                f"expected (N, {expected_input_shape[0]}, "
                f"{expected_input_shape[1]})"
            )

        if not np.isfinite(arr).all():
            raise ValueError(
                f"{name}: NaN/Inf detected after preprocessing."
            )

    for name in [
        "y_train",
        "y_val",
        "y_test"
    ]:
        labels = final_arrays[name]

        if not set(np.unique(labels)).issubset({0, 1}):
            raise ValueError(
                f"{name}: invalid labels {np.unique(labels)}"
            )

    # Train standardized mean/std should be approximately 0/1.
    train_std = final_arrays["X_train"]

    train_mean_check = float(
        train_std.mean()
    )
    train_std_check = float(
        train_std.std()
    )

    # --------------------------------------------------------
    # Save final summary
    # --------------------------------------------------------
    summary_rows = [
        {
            "Array": "X_train",
            "Shape": str(final_arrays["X_train"].shape),
            "Dtype": str(final_arrays["X_train"].dtype),
            "Mean": float(final_arrays["X_train"].mean()),
            "Std": float(final_arrays["X_train"].std())
        },
        {
            "Array": "X_val",
            "Shape": str(final_arrays["X_val"].shape),
            "Dtype": str(final_arrays["X_val"].dtype),
            "Mean": float(final_arrays["X_val"].mean()),
            "Std": float(final_arrays["X_val"].std())
        },
        {
            "Array": "X_test",
            "Shape": str(final_arrays["X_test"].shape),
            "Dtype": str(final_arrays["X_test"].dtype),
            "Mean": float(final_arrays["X_test"].mean()),
            "Std": float(final_arrays["X_test"].std())
        },
        {
            "Array": "X_rest_train",
            "Shape": str(final_arrays["X_rest_train"].shape),
            "Dtype": str(final_arrays["X_rest_train"].dtype),
            "Mean": float(final_arrays["X_rest_train"].mean()),
            "Std": float(final_arrays["X_rest_train"].std())
        },
        {
            "Array": "X_rest_val",
            "Shape": str(final_arrays["X_rest_val"].shape),
            "Dtype": str(final_arrays["X_rest_val"].dtype),
            "Mean": float(final_arrays["X_rest_val"].mean()),
            "Std": float(final_arrays["X_rest_val"].std())
        },
        {
            "Array": "X_rest_test",
            "Shape": str(final_arrays["X_rest_test"].shape),
            "Dtype": str(final_arrays["X_rest_test"].dtype),
            "Mean": float(final_arrays["X_rest_test"].mean()),
            "Std": float(final_arrays["X_rest_test"].std())
        }
    ]

    pd.DataFrame(summary_rows).to_csv(
        OUTPUT_DIR / "final_array_summary.csv",
        index=False,
        encoding="utf-8-sig"
    )

    # --------------------------------------------------------
    # Final console report
    # --------------------------------------------------------
    print("\n" + "=" * 80)
    print("FINAL PREPROCESSING COMPLETE")
    print("=" * 80)

    print("\n[Subject split]")
    print(f"Train subjects: {len(split['train'])}")
    print(f"Val subjects  : {len(split['val'])}")
    print(f"Test subjects : {len(split['test'])}")

    print("\n[Final shapes]")
    for name in [
        "X_train",
        "y_train",
        "X_val",
        "y_val",
        "X_test",
        "y_test",
        "X_rest_train",
        "X_rest_val",
        "X_rest_test"
    ]:
        print(
            f"{name:16s}: "
            f"{getattr(final_arrays[name], 'shape', None)}"
        )

    print("\n[Task labels]")
    for split_name, y_name in [
        ("Train", "y_train"),
        ("Val", "y_val"),
        ("Test", "y_test")
    ]:
        counts = pd.Series(
            final_arrays[y_name]
        ).value_counts().sort_index()

        print(
            f"{split_name:5s}: "
            f"Left(0)={int(counts.get(0, 0))}, "
            f"Right(1)={int(counts.get(1, 0))}"
        )

    print("\n[Standardization check: Train T1/T2]")
    print(
        f"mean = {train_mean_check:.6f}"
    )
    print(
        f"std  = {train_std_check:.6f}"
    )

    print("\n[Expected window timing]")
    print(
        ", ".join(
            f"{s:.2f}~{s + WINDOW_SEC:.2f}"
            for s in EXPECTED_WINDOW_STARTS
        )
    )

    print("\n[Output directory]")
    print(OUTPUT_DIR.resolve())

    print("\n✅ All requested preprocessing/QC checks completed.")


if __name__ == "__main__":
    main()

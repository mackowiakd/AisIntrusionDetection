# 🦠 ImmuneIDS: Bio-Inspired Network Intrusion Detection System

> **Status:** Active Development | **Looking for Internship/Junior roles!** 👋 
> I am a passionate Software Engineering student actively looking for an internship. If you are a recruiter or senior dev checking out my code, feel free to reach out!

An advanced, high-performance Network Intrusion Detection System (IDS) based on the Artificial Immune System (AIS) paradigm, specifically the **Negative Selection Algorithm (NSA)**. 

This project goes beyond simple scripting by implementing a highly optimized, hybrid architecture utilizing **C++ for lightning-fast data parsing** and **C# for multi-threaded algorithmic evaluation**.

## 🚀 Key Engineering Features

* **Hybrid Architecture (C# & C++ Interop):** Uses `P/Invoke` to bridge a custom C++ DLL (responsible for parsing and normalizing massive datasets) with a C# engine.
* **V-Detector Algorithm (Dynamic Radius):** Implemented an advanced version of NSA where detector radii adaptively grow based on Euclidean distance to the nearest 'Self' (normal traffic) node.
* **Adaptive Data Profiling:** Eliminates the "Empty Space Problem" in high-dimensional spaces (41 features) by dynamically calculating power distributions to spawn detectors near actual data clusters.
* **High-Performance Multithreading:** Utilizes `Parallel.ForEach` across available CPU cores with the **Double-Checked Locking** pattern to ensure thread-safe, lock-optimized distance calculations.
* **Memory Management:** Flat array memory allocation in C# passed directly via pointers to C++ to prevent Garbage Collector overhead during large dataset loading.

## 🧠 How It Works

1.  **Parsing (C++):** The `TrafficParser` DLL reads the `NSL-KDD` dataset, dynamically builds a schema based on ARFF headers, applies logarithmic transformations to squash outliers, and scales values (Min-Max scaler).
2.  **Training (C#):** The algorithm learns the "Self" (normal network behavior) and generates a resilient army of V-Detectors in a 41-dimensional space. Any detector overlapping with normal traffic is destroyed.
3.  **Evaluation (C#):** The system evaluates testing data. If a network packet falls within the radius of any surviving detector, it is flagged as an anomaly/attack.

## 🛠️ Tech Stack

* **C# / .NET 8** (Core Logic, Multithreading, Memory Management)
* **C++** (High-speed flat file parsing, DLL Export)
* **Visual Studio** (Native C++ & Managed C# debugging)

## 🏃‍♂️ How to Run the Project

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/YourUsername/AisIntrusionDetection.git](https://github.com/YourUsername/AisIntrusionDetection.git)
    ```
2.  **Download the Dataset:**
    * This project uses the [NSL-KDD Dataset](http://nsl.cs.unb.ca/NSL-KDD/).
    * Download `KDDTrain+_20Percent.ARFF` and `KDDTest+.ARFF`.
    * Place them in the project directory (make sure the path in `Program.cs` matches your local setup). *Note: The dataset is intentionally gitignored to keep the repository lightweight.*
3.  **Build the C++ DLL:**
    * Open the Solution in Visual Studio.
    * Right-click the `TrafficParser` project and select **Build** (Ensure you are on the `x64` architecture).
4.  **Run the C# Project:**
    * Set `AisIntrusionDetection` as the Startup Project and hit `Start`.

## 📈 Roadmap / Future Optimizations

- [x] Adaptive space profiling for detector generation.
- [x] V-Detector implementation (Dynamic Radius).
- [ ] **Dimensionality Reduction (PCA):** Reduce the 41-dimensional space to mitigate the Curse of Dimensionality.
- [ ] **Genetic Algorithm:** Replace random detector generation with crossover and mutation for optimal space coverage.
- [ ] 
## 📊 Experimental Results & Findings: The Curse of Dimensionality

In the initial phase of the project, the V-Detector algorithm was tested in the original, unreduced 41-dimensional space. We applied an innovative adaptive radius mechanism alongside power-law probability distribution profiling. The results provided hard empirical evidence of the destructive impact that the **Curse of Dimensionality** has on spherical anomaly detectors.

### 1. Evo<img width="3000" height="1800" alt="Wykres1_KosztEwolucyjny" src="https://github.com/user-attachments/assets/6457e985-2c41-4e74-a8cf-37c3dc1c9a61" />
lutionary Cost and Spatial Complexity

The chart above demonstrates the sheer difficulty of finding free space (valid gaps for detectors) in 41 dimensions. While the naive version (V.0) accepted coordinates randomly, our optimized algorithm (V.1) had to actively reject hundreds of thousands of candidates before successfully fitting detectors precisely to the boundaries of normal ("self") traffic.

### 2. Overgeneralization of Spherical Detectors
<img width="3600" height="2400" alt="Wykres3_Powierzchnia_3D" src="https://github.com/user-attachments/assets/b47e6b97-0b2d-4bf9-b4e3-5142c5de2967" />
<img width="3000" height="1800" alt="Wykres3 0_AnalizaProgu" src="https://github.com/user-attachments/assets/e54dcc1d-c421-4362-99fd-fc158fda96fa" />

We conducted a Grid Search over the hyperparameter space to analyze the trade-off between the True Positive (TP) rate and False Positives (FP). The 3D Fitness Landscape and heatmaps revealed a critical mathematical barrier:
* Due to the immense sparsity of the 41-dimensional space, adaptive detectors are allowed to grow to **gigantic radii**.
* During inference (testing on unseen data), these massive spheres inadvertently absorb natural, microscopic deviations present in normal network traffic.
* This phenomenon leads to an avalanche of False Positives (FP), causing the model's Accuracy to collapse to the 10-20% range.

**Architectural Conclusion:** Relying on Euclidean distance in 41 dimensions inevitably leads to distance concentration. Dimensionality reduction (PCA) is strictly required to tighten the gaps between network packets and generate highly accurate, smaller detectors.

## 📈 Roadmap / Future Optimizations

- [x] Adaptive space profiling for detector generation.
- [x] V-Detector implementation (Dynamic Radius).
- [ ] **Dimensionality Reduction (PCA):** Reduce the 41-dimensional space to mitigate the Curse of Dimensionality.
- [ ] **Genetic Algorithm:** Replace random detector generation with crossover and mutation for optimal space coverage.
---
*Created by [mackowiakd] - Open for collaboration and internship opportunities!*

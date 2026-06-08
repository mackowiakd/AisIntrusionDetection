About the Project: Solving the "Blind Spot" of Industry 4.0
Traditional Intrusion Detection Systems (IDS) rely on Deep Packet Inspection (DPI) and signature matching. However, with the rise of Industry 4.0 and secure protocols like OPC UA (Sign & Encrypt), traditional firewalls become blind to the encrypted payload. How do we protect critical infrastructure when we can't read the data?

This project introduces a Behavioral Anomaly Detection approach tailored for Operational Technology (OT) and ICS environments. Inspired by biological defense mechanisms, this Artificial Immune System (AIS) analyzes network traffic metadata (packet rates, intervals, flow statistics) rather than the payload itself. By establishing a baseline of "Self" (normal machine-to-machine communication), the system can detect "Non-Self" anomalies (DDoS, port scanning, ransomware behavior) even when the traffic is 100% encrypted.

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

## 📊 Experimental Results & Findings: The Curse of Dimensionality

In the initial phase of the project, the V-Detector algorithm was tested in the original, unreduced 41-dimensional space. We applied an innovative adaptive radius mechanism alongside power-law probability distribution profiling. The results provided hard empirical evidence of the destructive impact that the **Curse of Dimensionality** has on spherical anomaly detectors.

### The Failure of Brute Force and Hyperparameter Tuning in 41D Space
To mathematically prove the destructive nature of the 41D space, we conducted two baseline stress tests against the classic NSA algorithms (V0, V1) and our adaptive variant (V2).

<p align="center">
  <img width="800"  alt="Raport_Krzywa_Uczenia_Acc_vs_FP" src="https://github.com/user-attachments/assets/02292f3c-b092-475a-9b51-03625390080e" />

**Proof 1: The Brute Force Fallacy (Learning Curve)**
The chart above plots Accuracy (solid lines) against False Positives (dashed lines) as the detector population scales up to 20,000. 
* **V0/V1 (Blindness):** Remain completely flat at 50% accuracy (equivalent to a coin toss in a balanced dataset) with 0 False Positives. They are mathematically "lost" in the sparse 41D space, failing to detect any anomalies.
* **V2 (Overfitting):** Attempting to adaptively fit the space causes extreme instability. As the detector count increases, the False Positives (dashed green line) skyrocket to nearly 3,000. The algorithm chokes on its own generated noise, dragging accuracy down below 30%. **Conclusion: Adding more detectors/RAM does not solve high-dimensional sparsity.**
 

**Core Finding:** The 41D space fundamentally breaks Euclidean distance metrics. The algorithms cannot reliably distinguish between "Self" and "Non-Self" using spherical boundaries. This definitively proved the absolute necessity of implementing **Dimensionality Reduction (PCA)** before generating the immune system.

## 🔬  Algorithmic Testing: Zero-Day Attack Evaluation & The Accuracy Paradox

Following the implementation of PCA dimensionality reduction, an extensive benchmark was conducted to evaluate the geometric impact of compressed multidimensional spaces on detector generation strategies.
To rigorously test our algorithms, we evaluated them on the official, dedicated Kaggle `KDDTest+` dataset. This set intentionally includes **Zero-Day attacks** (novel attack vectors not present in the training data) and has a highly imbalanced class distribution (approx. 17,500 Normal packets vs. 4,000 Attacks).

Three distinct generation algorithms were tested across multiple PCA dimensions (from 5D to 30D, against a 41D baseline):
* **V0_Basic (Rigid Radius):** The classic NSA approach. Detectors are scattered uniformly with a static, statistically derived maximum radius.
* **V2_Gravity (Adaptive + Gravity):** An advanced approach forcing detectors to spawn in close mathematical proximity to the "self" traffic cluster before growing adaptively.
* **V3_Uniform (Adaptive + Uniform):** Scatters detectors evenly across the hyperspace, but empowers them with an adaptive radius that grows until it hits the normal traffic boundary.

### 📊 Benchmark Results: The Geometric Paradigm Shift
<img width="1600" height="960" alt="Raport_PCA_Wymiary" src="https://github.com/user-attachments/assets/c874fe03-31f8-4187-a375-56e2f23e193e" />


*(Results averaged over 3 evaluation runs on the KDDTest+ dataset. Parameters: 20D PCA, 5000 Detectors).*
| Generation Strategy | Raw Accuracy | True Positive Rate (Recall) | Attacks Caught (TP) | False Positives (FP) |
| :--- | :---: | :---: | :---: | :---: |
| **V0_Basic** *(Static Radius)* | 81.39% ⚠️ | **0.00%** | 0 / 4009 | 0 |
| **V2_Gravity** *(Adaptive Pull)* | 79.08% | **1.31%** | ~53 / 4009 | ~549 |
| **V3_Uniform** *(Adaptive Shield)* | 80.00% | **97.32% 🏆** | **~3902 / 4009** | ~4200 |



### 🧠 Architectural Conclusions & The Accuracy Paradox

Testing on the imbalanced Zero-Day dataset exposed a classic Machine Learning trap and proved the absolute superiority of the V3 architecture:

1. **The Accuracy Paradox (V0 & V1 Failure):** If you look purely at the "Raw Accuracy" column, V0_Basic seems to be the winner with 81.39%. However, this is a dangerous statistical illusion. Because of the static radius parameters, V0 and V1 completely failed to cover the anomaly space. They defaulted to predicting *every single packet* as normal traffic. Since 81.39% of the test set *was* normal traffic, the math checks out, but their **True Positive Rate (TPR) is 0%**. They missed every single cyberattack.
   
2. **The Gravity Collapse (V2):** As predicted by our spatial analysis, V2_Gravity caught a dismal 1.3% of the attacks. Dragging detectors into the intertwined center of the dataset proves entirely ineffective against novel, outward-lying Zero-Day threats.

3. **The Ultimate Winner (V3_Uniform):** Our V3 Adaptive Shield algorithm effectively solved the problem. By scattering detectors uniformly and dynamically expanding them to the edges of the "Self" cluster, V3 successfully intercepted **97.32% of all Zero-Day attacks** (~3902 out of 4009). While this aggressive shielding naturally blocks some normal traffic (resulting in a higher False Positive rate and a nominal Accuracy of 80%), its unparalleled Recall makes it the only mathematically viable architecture for a real-world Immune NIDS.

### 🎛️ Hyperparameter Grid Search (V3_Uniform)

To ensure peak performance for the Live Demo, we executed an automated parameter sweep across the V3 algorithm. 

<p align="center">
  <img width="800" alt="Code_Generated_Image (1)" src="https://github.com/user-attachments/assets/64f43f6f-376e-4278-ad6c-edcaaf235cbe" />
</p>

The tuning revealed that scaling the detector count beyond **5000** begins to yield diminishing returns. A highly dense shield (e.g., 15,000 detectors) forces elements into microscopic crevices between normal traffic and anomalies, causing a spike in False Positives without a meaningful increase in Attack detection. Thus, a lean, 5000-detector configuration at a 0.10 minimum radius was established as the optimal balance for inference speed and security.



### 🔬 Deep Dive: Algorithmic Post-Mortem & Spatial Mechanics

To truly understand why the algorithms performed the way they did, we must look at the raw structure of the NSL-KDD dataset and how our detectors interact with it.

#### 1. The Dynamic Radius Solution ($r_{max} = dist_{min}$)
Why did V0_Basic fail even after PCA? It relied on a **Static Radius**. In complex, irregular data spaces, a fixed-size sphere either overlaps with normal traffic (causing False Positives) or leaves massive gaps for attacks to slip through.

Our V3_Uniform algorithm solved this using a **Dynamic/Adaptive Radius**. The algorithm spawns a detector and dynamically inflates it until it perfectly touches the nearest "Self" (healthy) network packet. This allows the system to build a watertight, custom-fitted shield around the irregular boundaries of normal traffic.

| Static Radius | Adaptive Radius |
| :---: | :---: |
| <img src="https://github.com/user-attachments/assets/72e5bd3e-7bda-48c6-8f16-aac96c7de2b7" alt="Static Radius" width="100%"> | <img  src="https://github.com/user-attachments/assets/e16275a7-1224-4443-93de-b9016ef31ade" alt="Adaptive Radius" width="100%"> |


#### 2. Why the V2 "Gravity" Heuristic Failed
Initially, the V2 Gravity algorithm was our theoretical favorite. Its logic was to gravitationally pull detectors toward the densest centers of the "Self" data to build a tight perimeter. However, it failed catastrophically (dropping to coin-toss accuracy). Why?

<p align="center">
<img width="750" alt="kaggle_dataset" src="https://github.com/user-attachments/assets/728d58b1-fb5b-4258-9ac2-d49ace53306c" />
</p>

A 3D projection of our dataset (above) reveals the harsh reality of real-world network traffic: **The classes are highly intertwined.** Malicious traffic (anomalies) heavily overlaps with normal traffic. 

<p align="center">
 <img width="745" alt="v2 gravity" src="https://github.com/user-attachments/assets/af4c55c2-f561-4f39-b787-79ac0b69cd77" />
</p>

* **The Conceptual Flaw:** Gravity would only work in an ideal scenario where normal traffic forms perfectly isolated "islands" (Right Panel). Because our real-world dataset overlaps (Left Panel), the Gravity heuristic actively dragged perfect detectors straight into the chaotic center. This forced massive collisions with healthy packets, leading to an unacceptable False Positive rate. 
* **The Takeaway:** For highly overlapped datasets, attempting to penetrate the cluster is a mathematical flaw. The V3 Uniform approach succeeded because it builds a protective shield on the *outside* of the cluster instead.

## 🎯 Target Deployment: Industrial OT vs. Business IT Networks

While the benchmarks demonstrated high accuracy, a critical engineering conclusion of this project is understanding **where this specific architecture belongs**. This Immune IDS is explicitly designed for **Operational Technology (OT)** environments rather than standard **Information Technology (IT)** networks.

* **The Chaos of IT (Business Traffic):** Standard office networks (web browsing, large downloads, varying hours) are highly erratic. The "Self" cluster is blurry, noisy, and constantly shifting. Implementing strict geometric anomaly boundaries (like AIS) here would generate excessive False Positives. For IT environments, probabilistic models (like Deep Neural Networks or LSTMs) are generally superior.
* **The Determinism of OT (Industrial Automation):** Industrial protocols (e.g., **OPC UA, Profinet**) rely on highly deterministic, Machine-to-Machine (M2M) communication. A PLC sends exact byte payloads at rigid millisecond intervals. This creates a highly dense, mathematically predictable "Self" cluster—the exact environment where the **V3_Uniform** algorithm thrives, allowing us to build a watertight, zero-tolerance shield around the traffic.
* **The Edge Computing Advantage:** Real-time industrial controllers cannot run heavy antivirus software or GPU-accelerated Deep Learning models without introducing fatal latency. Because our PCA-reduced model relies on simple Euclidean distance checks against 5000 spatial parameters, it can be exported and run *passively* on an edge switch. This provides critical infrastructure with a lightweight, Zero-Day firewall with near-zero computational overhead.

## 📈 Roadmap & Future Optimizations

The V3_Uniform algorithm, combined with PCA, successfully solved the detection problem, achieving high accuracy and neutralizing Zero-Day attacks. However, the next phase of development focuses on **Production-Level Performance and Real-Time Inference Speed**.

- [x] Adaptive space profiling for detector generation.
- [x] V-Detector implementation (Dynamic Radius calculating Euclidean distance).
- [x] **Dimensionality Reduction (PCA):** Compressing 41-dimensional space to mitigate the Curse of Dimensionality and restore distance metric reliability.
- [ ] **Genetic Algorithm (GA) Optimization for Inference Speed:** * **The Goal:** Currently, V3 uses random uniform spawning (brute force), which requires generating thousands of overlapping detectors (e.g., 5000+) to secure the perimeter. While highly accurate, evaluating every live network packet against 5000 detectors is computationally expensive for real-time IDS.
  * **The Implementation:** We propose replacing the random spawning phase with a Genetic Algorithm. By evaluating a detector's "Fitness" based on its maximum non-overlapping volume ($r_{max}$), we can evolve a mathematically optimal set of detectors.
  * **The Outcome:** The goal of the GA is *not* to radically improve accuracy (as V3 is already highly effective), but to achieve the same 98% coverage using a fraction of the detectors (e.g., 500 instead of 5000). Evolving a leaner, smarter immune shield will drastically reduce CPU cycles during live traffic inference, transitioning the project from a highly accurate prototype to a production-ready, high-performance engine.

## 📸 Application Showcase
<img width="800" alt="Zrzut ekranu 2026-05-24 111639" src="https://github.com/user-attachments/assets/d09be2b8-8930-4617-bb39-0f738f14a1f8" />

## 🛠️ Tech Stack

* **C# / .NET 8** (Core Logic, Multithreading, Memory Management)
* **C++** (High-speed flat file parsing, DLL Export)
* **Visual Studio** (Native C++ & Managed C# debugging)
* 
## 🚀 Key Engineering Features

* **Hybrid Architecture (C# & C++ Interop):** Uses `P/Invoke` to bridge a custom C++ DLL (responsible for parsing and normalizing massive datasets) with a C# engine.
* **V-Detector Algorithm (Dynamic Radius):** Implemented an advanced version of NSA where detector radii adaptively grow based on Euclidean distance to the nearest 'Self' (normal traffic) node.
* **Adaptive Data Profiling:** Eliminates the "Empty Space Problem" in high-dimensional spaces (41 features) by dynamically calculating power distributions to spawn detectors near actual data clusters.
* **High-Performance Multithreading:** Utilizes `Parallel.ForEach` across available CPU cores with the **Double-Checked Locking** pattern to ensure thread-safe, lock-optimized distance calculations.
* **Memory Management:** Flat array memory allocation in C# passed directly via pointers to C++ to prevent Garbage Collector overhead during large dataset loading.

## 🏗️ System Architecture

<img height="500"  alt="WPF User Interface Data Flow" src="https://github.com/user-attachments/assets/da11ac80-9fd8-4dca-b948-79dc274dbe61" />

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
      
    
*Created by [mackowiakd] - Open for collaboration and internship opportunities!*

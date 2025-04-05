# Eye Tracking in Virtual Classroom

## Project Overview

This interdisciplinary project explores how eye-tracking technology can improve virtual classroom experiences by measuring student attention and mental load. Using Unity for the front-end VR experience and a Python Flask server for the backend, this system captures gaze data from VR users in real-time and processes it to offer cognitive feedback. The ultimate goal is to improve learning outcomes by alerting students and instructors when distraction or cognitive overload occurs.

## System Architecture

The project is built with two primary components:

- **Unity VR Environment**: Provides a virtual classroom with educational materials and avatars.
- **Python Flask Server**: Receives, analyzes, and visualizes gaze data from Unity.

The Unity application saves gaze data to `.csv` format in real time and sends it to the Python server at specific frame intervals, which can be adjusted in the game settings. This data includes timestamps, gaze positions, pupil diameters, and the object the user is focusing on.

## Features

- **Fixation Rate Calculation**: Measures how long a student focuses on a specific object.
- **Saccade Rate Calculation**: Tracks rapid eye movements between focal points.
- **Distraction Detection**: Identifies when a student looks away from the task for a sustained period.
- **Cognitive Overload Detection**: Analyzes combined metrics like fixation, saccade, and pupil dilation to detect cognitive strain.
- **Real-time Alerts**: Notifies the instructor or logs data when a student is distracted or overloaded.
- **User Tracking**: All gaze data is tagged with a `User ID` for individual analysis.

## Definitions of Key Concepts

- **Fixation**: A fixation occurs when the eyes remain focused on a single point or object for a period of time. Longer fixations often indicate cognitive processing and attention.
- **Saccade**: A saccade is a quick, simultaneous movement of both eyes between two or more phases of fixation in the same direction. Frequent saccades may indicate scanning or lack of focus.
- **Distraction**: Distraction is defined as the user's gaze shifting away from the relevant task or object for a duration longer than a defined threshold.
- **Cognitive Overload**: Cognitive overload happens when the mental processing capacity of the user is exceeded, often signaled by high fixation times, reduced saccades, and enlarged pupil size.

<p align="center">
<img src="https://github.com/Cdelim/EyeTrackingVR/blob/main/ReadmeImages/Gaze_Distribution.png" width="300" height="200"/>

## How Distraction and Overload Are Detected

On the server-side, key Python functions process the incoming CSV data. Below are two of the core algorithms:

### `cognitive_overload_detection(results_dict)`
```python
def cognitive_overload_detection(results_dict):
    fixation_mean = results_dict['eye_movement_statistics']["fixation"]["mean"]
    saccade_mean = results_dict['eye_movement_statistics']["saccade"]["mean"]
    pupil_diameter_mean = results_dict['pupil_data']['normalized_statistics']["mean_pupil_diameter"]

    if pd.isna(fixation_mean) or pd.isna(saccade_mean) or pd.isna(pupil_diameter_mean):
        return False
    cognitive_overload = (
        fixation_mean > FIXATION_OVERLOAD_THRESHOLD
        and saccade_mean < SACCADE_UNDERLOAD_THRESHOLD
        and pupil_diameter_mean > PUPIL_OVERLOAD_THRESHOLD
    )
    return cognitive_overload
```

This function determines whether a student is experiencing cognitive overload by analyzing fixation length, saccade frequency, and average pupil dilation.

### `detect_distraction(df)`
```python
def detect_distraction(df):
    distraction_events = []
    distraction_start = None

    for index, row in df.iterrows():
        if row['GazedObject'] != row['Task']:
            if distraction_start is None:
                distraction_start = row['TimeStamp']
            elif row['TimeStamp'] - distraction_start > DISTRACTION_TIME_TRESHOLD:
                distraction_events.append((distraction_start, row['TimeStamp']))
                distraction_start = None
                return True
        else:
            distraction_start = None
    print(distraction_events)
    if(len(distraction_events) != 0):
        return True
    return False
```


<img src="https://github.com/Cdelim/EyeTrackingVR/blob/main/ReadmeImages/Distraction_Panel.png" width="300" height="300"/>

This logic checks whether the user’s gaze has wandered away from the target task for too long, indicating distraction.

## Technologies Used

- **Unity** – Used to build the immersive VR classroom.
- **Varjo HMD** – High-end VR headset with integrated eye-tracking.
- **Varjo Eye Tracking Sensors** – Captures gaze and pupil data.
- **ReadyPlayerMe** – Avatar generation system for VR users.
- **Python Flask** – Hosts backend services and runs detection algorithms.
- **Pandas, NumPy** – Data analysis and manipulation tools.

## Installation & Setup

### Prerequisites

- Unity (latest version recommended)
- Varjo HMD and eye-tracking sensors
- Python 3.x
- Flask, pandas, numpy libraries

### Setup Steps

1. **Clone the repository:**
   ```sh
   git clone https://github.com/your-repo-url.git
   cd eye-tracking-vr
   ```
2. **Install Python dependencies:**
   ```sh
   pip install flask numpy pandas
   ```
3. **In Unity:**
   - Import the Varjo and ReadyPlayerMe SDKs
   - Add your avatar prefab to the scene
   - Set the user camera to follow gaze direction
   - Mark objects to track gaze on
   - Assign a unique `User ID`
   - Adjust the data send frequency in the inspector
4. **Run the Python Flask server:**
   ```sh
   python assets/ServerSide/server.py
   ```
5. **Play the scene in Unity and observe real-time data analysis.**

### Unity Package Setup (Alternative)

If you prefer a streamlined setup, the project also supports importing via a Unity package file:

1. Download the .unitypackage: Get the pre-packaged scene from the Releases section or your shared drive.

2. Open Your Unity Project:

    - Create a new Unity project (preferably 3D URP or HDRP if using Varjo).

    - Ensure the Varjo plugin is installed (via Unity Package Manager or Varjo Plugin site).

    - Import the Package:

    - In Unity, go to Assets > Import Package > Custom Package...

    - Select the downloaded .unitypackage.

    - Make sure all files are checked and click Import.

3. Scene Configuration:

    - Open the imported VRScene.unity scene.

    - Assign your Varjo HMD and ReadyPlayerMe avatar in the scene hierarchy.

    - Ensure GazeRayController.cs and related scripts are attached to the Main Camera. (Check the avatar in the VRScene.)


<img src="https://github.com/Cdelim/EyeTrackingVR/blob/main/ReadmeImages/Gaze_Controller.png" width="300" height="200"/>

4. Customize the inspector settings:

    - User ID

    - Data send frequency

    - CSV output path (optional override)



<img src="https://github.com/Cdelim/EyeTrackingVR/blob/main/ReadmeImages/TaskSettings.png" width="300" height="200"/>

</p>

5. Connect to Python Server:

    - Run the Flask server as described in the Setup Steps.

    - The Unity scene will send gaze data automatically if the server is running.

✅ This setup is ideal for quick deployment or testing in new environments.

## Usage Workflow

- Launch Unity and enter play mode with Varjo HMD.
- The Unity client logs gaze data and sends it every specific amount of frame to the server.
- Server functions process and analyze user attention patterns.
- Distraction and overload alerts are generated if thresholds are exceeded.
- Reports and logs can be accessed for each user.

## Results and Benefits
- Improved Student Engagement: By identifying attention patterns, the system helps instructors adapt lessons in real time to keep students more engaged.
- Instructor Insights: Teachers gain clear, data-driven reports on each student’s focus and cognitive state, allowing more personalized instruction.
- Early Intervention: Distraction and cognitive overload alerts enable timely support before students fall behind.
- Enhanced Learning Outcomes: Encouraging sustained attention and providing adaptive pacing improves retention and comprehension.
- Support for Educational Researchers: Eye-tracking metrics offer valuable data for studying learning behaviors in virtual environments.
- Scalability for Remote Education: Provides a framework for tracking engagement in large-scale online or hybrid classrooms.

## Future Improvements

- AI-Powered Analysis: Use machine learning to predict attention loss trends and personalize feedback loops.
- Visual Heatmaps: Exportable heatmaps of user gaze to analyze focus areas and curriculum impact.
- Cross-Platform Compatibility: Support for various VR/AR devices (e.g., Meta Quest, HTC Vive).
- Instructor Dashboard: In-app real-time analytics with charts and attention scores for classroom monitoring.
- Custom Event Triggers: Teachers can define specific tasks or time ranges to measure attention more granularly.


## Contributors

- Cem Bektasoglu


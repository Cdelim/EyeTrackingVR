Eye Tracking in Virtual Classroom

Project Overview

This project aims to enhance virtual education by integrating eye-tracking technology into a classroom environment. By leveraging eye-tracking sensors within a VR setting, we can measure student engagement, detect distractions, and analyze cognitive load. The system is designed to provide real-time feedback to both students and teachers, improving learning outcomes.

Features

Fixation Rate Calculation: Measures how long a student focuses on a specific object.

Saccade Rate Calculation: Tracks rapid eye movements between focal points.

Distraction Detection: Identifies when a student looks away from important learning material.

Cognitive Overload Detection: Assesses when the student is overwhelmed by too much information.

Real-time Alerts: Notifies instructors when students lose focus.

User Tracking: Each student's gaze data is processed individually for analysis.

Technologies Used

Unity – Used to build the virtual classroom environment.

Varjo Head-Mounted Device (HMD) – Provides high-fidelity VR experiences.

Varjo Eye Tracking Sensors – Captures gaze data and eye movement.

ReadyPlayerMe – Generates avatars and animations for users.

Python Flask – Handles server-side data processing.

Installation & Setup

Prerequisites

Unity (latest version recommended)

Varjo HMD and eye-tracking sensors

Python 3.x

Flask library

Steps to Setup

Clone the repository:

git clone https://github.com/your-repo-url.git
cd eye-tracking-vr

Install required Python dependencies:

pip install flask numpy pandas

Open the Unity project and set up the following:

Import the Varjo SDK and ReadyPlayerMe SDK.

Set the Avatar Camera to track user gaze.

Define interactive objects in the scene.

Assign a User ID to track individual students.

Run the Flask server:

python server.py

Start the Unity scene and test the eye-tracking features.

Usage

Launch the virtual classroom in Unity.

Wear the Varjo HMD to activate eye tracking.

Engage with the environment while the system records gaze data.

The server processes data and provides real-time alerts for distractions.

Teachers and students receive reports based on engagement levels.

Results & Benefits

Better Student Engagement Analysis: Helps identify where students lose focus.

Improved Teaching Strategies: Teachers can adjust their methods based on real-time feedback.

More Effective Virtual Learning: Makes VR-based education more interactive and responsive.

Future Improvements

Support for additional VR headsets.

Integration with AI to predict student performance trends.

More detailed gaze heatmaps for visual analysis.

Contributors

Cem Bektasoglu

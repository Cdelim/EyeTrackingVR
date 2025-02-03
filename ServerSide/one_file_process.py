import os
import pandas as pd
import numpy as np
from collections import defaultdict
import json
from scipy.signal import savgol_filter
import matplotlib.pyplot as plt
import pickle
from flask import Flask, request, jsonify



# Global Parameters
#PUPIL PARAMETERS
WINDOW_LENGTH = 11  # Example window length for smoothing
POLYORDER = 2  # Example polynomial order for smoothing
BASELINE_DURATION = 1  # in seconds

#FIXATION&SACCADE PARAMETERS
MIN_FIXATION_DURATION = 0.1  # Minimum fixation duration in seconds
MAX_FIXATION_DURATION = 0.5  # Maximum fixation duration in seconds

MIN_SACCADE_DURATION = 0.02  # Minimum saccade duration in seconds
MAX_SACCADE_DURATION = 0.1 # Maximum saccade duration in seconds

HEAD_VELOCITY_THRESHOLD = 7  # Head velocity threshold for fixation
GAZE_VELOCITY_FIXATION_THRESHOLD = 30  # Gaze velocity threshold for fixation
GAZE_VELOCITY_SACCADE_THRESHOLD = 40  # Gaze velocity threshold for saccade

# Correct answers for the video games questionnaire
video_games_correct_answers = {
    '1': 'Tennis for Two',
    '2': 'Pong',
    '3': 'Space Invaders',  # Corrected answer
    '4': 'It broke down political and cultural barriers during the Cold War',
    '5': 'The introduction of 3D graphics',
    '6': 'Sony',
    '7': 'The introduction of online gaming and digital distribution',
    '8': 'The emergence of esports',
    '9': 'Further integration of VR and AR',
    '10': 'Storytelling, education, and virtual social interaction'
}

# Correct answers for the double-slit experiment questionnaire
double_slit_correct_answers = {
    '1': 'Quantum Mechanics',
    '2': 'Thomas Young',
    '3': 'As both particles and waves',
    '4': 'Interference',
    '5': 'Wave-particle duality',
    '6': 'The particle behaves like a classical particle',
    '7': 'After they are measured',
    '8': 'Superposition',
    '9': 'It highlights the complexities and wonders of the quantum world',  # Depending on context, could vary
    '10': '1897'
}

# Mapping for familiarity responses
familiarity_mapping = {
    "Not at all": 1,
    "Very little knowledge": 2,
    "Moderate understanding": 3,
    "Quite familiar": 4,
    "Very knowledgeable": 5
}
def calculate_scores(results, correct_answers):
    """
    Calculate scores for each participant based on their responses.

    Parameters:
    - results (dict): Dictionary containing participant responses.
    - correct_answers (dict): Dictionary containing correct answers for each question.
    - familiarity_mapping (dict): Mapping of familiarity responses to numerical scores.

    Returns:
    - scores (dict): Dictionary containing quiz scores and familiarity scores for each participant.
    """
    scores = {}

    # Iterate through each participant's results
    for participant_id, responses in results.items():
        if participant_id == 'explanation':  # Skip the explanation key
            continue

        # Get familiarity score
        familiarity_text = responses.get('0')  # Assuming '0' is the key for the familiarity question
        familiarity_score = familiarity_mapping.get(familiarity_text, None)  # Convert text to score
        quiz_score = 0

        # Calculate quiz score based on correct answers
        for question_id, correct_answer in correct_answers.items():
            if question_id in responses and responses[question_id] == correct_answer:
                quiz_score += 1

        # Store familiarity and quiz scores in the dictionary
        scores[participant_id] = {
            'familiarity': familiarity_score,
            'quiz_score': quiz_score
        }

    return scores

def process_pupil_diameter_data(df):
    """
    Processes pupil diameter data, including smoothing, baseline correction,
    normalization (with and without baseline correction), and calculating statistics.

    Parameters:
    - df (DataFrame): DataFrame containing TimeStamp and AvgPupilDiameter columns.

    Returns:
    - pupil_data_dict (dict): Dictionary containing processed pupil data and statistics.
    """

    # Extract pupil diameter data
    pupil_df = get_pupil_diameter_data(df)
    pupil_df.fillna(value=0)
    # Step 1: Smooth Pupil Diameter Data
    window_length = WINDOW_LENGTH  # Example window length for smoothing
    polyorder = POLYORDER  # Example polynomial order for smoothing
    pupil_df['SmoothedPupilDiameter'] = savgol_filter(pupil_df['AvgPupilDiameter'], window_length, polyorder,
                                                      0, 1.0,
                                                    -1, 'nearest', 0.0)

    # Step 2: Baseline Correction (Subtractive)
    baseline_duration = BASELINE_DURATION  # in seconds
    baseline_end_time = pupil_df['TimeStamp'].iloc[0] + baseline_duration
    baseline_data = pupil_df[pupil_df['TimeStamp'] <= baseline_end_time]
    baseline_value = baseline_data['SmoothedPupilDiameter'].mean()

    pupil_df['BaselineCorrectedPupilDiameter'] = pupil_df['SmoothedPupilDiameter'] / baseline_value

    # Step 3: Normalize Baseline-Corrected Pupil Diameter
    min_corrected = pupil_df['BaselineCorrectedPupilDiameter'].min()
    max_corrected = pupil_df['BaselineCorrectedPupilDiameter'].max()
    pupil_df['NormalizedCorrectedPupilDiameter'] = (
        pupil_df['BaselineCorrectedPupilDiameter'] - min_corrected
    ) / (max_corrected - min_corrected)

    # Step 4: Normalize Pupil Diameter Without Baseline Correction
    min_original = pupil_df['SmoothedPupilDiameter'].min()
    max_original = pupil_df['SmoothedPupilDiameter'].max()
    pupil_df['NormalizedPupilDiameter'] = (
        pupil_df['SmoothedPupilDiameter'] - min_original
    ) / (max_original - min_original)

    # Analyze statistics for normalized corrected data
    normalized_corrected_stats = {
        'min_pupil_diameter': pupil_df['NormalizedCorrectedPupilDiameter'].min(),
        'max_pupil_diameter': pupil_df['NormalizedCorrectedPupilDiameter'].max(),
        'mean_pupil_diameter': pupil_df['NormalizedCorrectedPupilDiameter'].mean(),
        'std_pupil_diameter': pupil_df['NormalizedCorrectedPupilDiameter'].std(),
        'count': pupil_df['NormalizedCorrectedPupilDiameter'].count()
    }

    # Analyze statistics for normalized data without baseline correction
    normalized_stats = {
        'min_pupil_diameter': pupil_df['NormalizedPupilDiameter'].min(),
        'max_pupil_diameter': pupil_df['NormalizedPupilDiameter'].max(),
        'mean_pupil_diameter': pupil_df['NormalizedPupilDiameter'].mean(),
        'std_pupil_diameter': pupil_df['NormalizedPupilDiameter'].std(),
        'count': pupil_df['NormalizedPupilDiameter'].count()
    }

    # Prepare the result dictionary
    pupil_data_dict = {
        'smoothed_pupil_data': pupil_df[['TimeStamp', 'SmoothedPupilDiameter']].to_dict(orient='list'),
        'baseline_corrected_pupil_data': pupil_df[['TimeStamp', 'BaselineCorrectedPupilDiameter']].to_dict(orient='list'),
        'normalized_corrected_pupil_data': pupil_df[['TimeStamp', 'NormalizedCorrectedPupilDiameter']].to_dict(orient='list'),
        'normalized_pupil_data': pupil_df[['TimeStamp', 'NormalizedPupilDiameter']].to_dict(orient='list'),
        'normalized_corrected_statistics': normalized_corrected_stats,
        'normalized_statistics': normalized_stats
    }

    return pupil_data_dict






def get_pupil_diameter_data(df):


    df = df[df['GazeStatus'] == 'VALID']
    # Extract relevant columns for pupil diameter
    pupil_columns = ['TimeStamp', 'LeftPupilDiameterInMM', 'RightPupilDiameterInMM']

    # Select the relevant columns and convert them to float
    pupil_df = df[pupil_columns].apply(pd.to_numeric, errors='coerce')

    # Calculate the average pupil diameter for both eyes
    pupil_df['AvgPupilDiameter'] = pupil_df[['LeftPupilDiameterInMM', 'RightPupilDiameterInMM']].mean(axis=1)

    return pupil_df


def analyze_pupil_diameter(pupil_df):
    # Calculate overall statistics for pupil diameter (without separating fixations and saccades)
    overall_stats = {
        'min_pupil_diameter': pupil_df['AvgPupilDiameter'].min(),
        'max_pupil_diameter': pupil_df['AvgPupilDiameter'].max(),
        'mean_pupil_diameter': pupil_df['AvgPupilDiameter'].mean(),
        'std_pupil_diameter': pupil_df['AvgPupilDiameter'].std(),
        'count': pupil_df['AvgPupilDiameter'].count()
    }

    return overall_stats




def parse_filename(filename):
    # Split the filename to extract the necessary components
    parts = filename.split(' - ')
    date_part, participant_case, session_part = parts[0], parts[1], parts[2]

    # Extract participant ID and case number
    participant_id = participant_case.split('C')[0][1:]  # Extracting ID between 'P' and 'C'
    case_number = int(participant_case.split('C')[1][0])  # Extracting case number after 'C'
    session_number = int(session_part.split('.')[0].split('-')[-1].strip())  # Extracting session number from "- 1.csv"

    # Initialize the dictionary to store results
    result = {
        'participant_id': participant_id,
        'case_number': case_number,
        'session': session_number
    }

    # Determine QnA and Topic based on case number and session number
    if case_number == 1:
        if session_number == 1:
            result['condition'] = 'nonqna'
            result['topic'] = 'doubleslit'
        elif session_number == 2:
            result['condition'] = 'qna'
            result['topic'] = 'videogames'
    elif case_number == 2:
        if session_number == 1:
            result['condition'] = 'qna'
            result['topic'] = 'doubleslit'
        elif session_number == 2:
            result['condition'] = 'nonqna'
            result['topic'] = 'videogames'
    elif case_number == 3:
        if session_number == 1:
            result['condition'] = 'qna'
            result['topic'] = 'videogames'
        elif session_number == 2:
            result['condition'] = 'nonqna'
            result['topic'] = 'doubleslit'
    elif case_number == 4:
        if session_number == 1:
            result['condition'] = 'nonqna'
            result['topic'] = 'videogames'
        elif session_number == 2:
            result['condition'] = 'qna'
            result['topic'] = 'doubleslit'

    return result



def get_valid_head_and_gaze_movements(df):
    # Extract the relevant columns for head and gaze movements

    valid_gaze_df= df[df['GazeStatus']=='VALID']

    head_and_gaze_columns = [
        'TimeStamp', 'HeadPositionX', 'HeadPositionY', 'HeadPositionZ',
        'HeadDirectionX', 'HeadDirectionY', 'HeadDirectionZ',
        'CombinedGazeForwardX', 'CombinedGazeForwardY', 'CombinedGazeForwardZ',
        'CombinedGazePositionX', 'CombinedGazePositionY', 'CombinedGazePositionZ'
    ]

    # Select the relevant columns and convert them to float, except 'GazeStatus'
    valid_gaze_df = valid_gaze_df[head_and_gaze_columns].apply(lambda x: pd.to_numeric(x, errors='coerce') if x.name != 'GazeStatus' else x)

    return valid_gaze_df


def analyze_eye_movements(eye_movement_df):
    # Ensure the 'MovementType' and 'FrameDuration' columns exist in the DataFrame
    if 'MovementType' not in eye_movement_df.columns or 'FrameDuration' not in eye_movement_df.columns:
        raise ValueError("The DataFrame must contain 'MovementType' and 'FrameDuration' columns.")

    # Filter fixations and saccades
    fixations = eye_movement_df[eye_movement_df['MovementType'] == 'fixation']
    saccades = eye_movement_df[eye_movement_df['MovementType'] == 'saccade']

    # Calculate statistics for fixations
    fixation_durations = fixations['FrameDuration']
    fixation_stats = {
        'min_duration': fixation_durations.min(),
        'max_duration': fixation_durations.max(),
        'mean_duration': fixation_durations.mean(),
        'total_duration': fixation_durations.sum(),
        'count': fixation_durations.count()
    }

    # Calculate statistics for saccades
    saccade_durations = saccades['FrameDuration']
    saccade_stats = {
        'min_duration': saccade_durations.min(),
        'max_duration': saccade_durations.max(),
        'mean_duration': saccade_durations.mean(),
        'total_duration': saccade_durations.sum(),
        'count': saccade_durations.count()
    }

    # Total time in the DataFrame
    total_time = eye_movement_df['FrameDuration'].sum()

    # Calculate percentages
    fixation_percentage = fixation_stats['total_duration'] / total_time * 100
    saccade_percentage = saccade_stats['total_duration'] / total_time * 100

    # Add percentage information
    fixation_stats['percentage_time'] = fixation_percentage
    saccade_stats['percentage_time'] = saccade_percentage

    # Combine results
    statistics = {
        'fixation_stats': fixation_stats,
        'saccade_stats': saccade_stats,
        'total_time': total_time
    }

    return statistics


def process_log_file(file_path):
    if os.path.isfile(file_path):
        with open(file_path, 'r') as file:
            lines = file.readlines()

        rows = [line.strip().split(';') for line in lines]
        headers = rows[0]
        data = rows[1:]
        for i in range(len(data)):
            if len(data[i]) > len(headers):
                data[i] = data[i][:-1]

        df = pd.DataFrame(data, columns=headers)

        if df.empty:
            print(f"File {file_path} is empty.")
            return None

        # Check if 'TimeStamp' column exists
        if 'TimeStamp' not in df.columns:
            print(f"The 'TimeStamp' column is missing in the file {file_path}.")
            return None

        # Convert 'TimeStamp' to numeric and handle errors
        df['TimeStamp'] = pd.to_numeric(df['TimeStamp'], errors='coerce') / 1_000.0  # Convert to seconds

        # Check if 'TimeStamp' column has valid data
        if df['TimeStamp'].isnull().all():
            print(f"All 'TimeStamp' values are invalid in the file {file_path}.")
            return None


        total_duration_seconds = df['TimeStamp'].iloc[-1] - df['TimeStamp'].iloc[0]
        print(f"Total Duration (Seconds): {total_duration_seconds}")

        total_duration_minutes = total_duration_seconds / 60.0
        print(f"Total Duration (Minutes): {total_duration_minutes}")

        total_frames = df.shape[0]
        average_fps = total_frames / total_duration_seconds if total_duration_seconds > 0 else 0
        print(f"Average FPS: {average_fps}")

        df['FrameDuration'] = df['TimeStamp'].diff().fillna(0)
        df['GazeObjectDuration'] = df.groupby('GazedObject')['FrameDuration'].transform('sum')
        total_gaze_duration = df['GazeObjectDuration'].sum()
        normalized_durations = df.groupby('GazedObject')['GazeObjectDuration'].sum() / total_gaze_duration

        result_dict = {}
        result_dict['total_duration_minutes'] = total_duration_minutes
        result_dict['column_names'] = df.columns.tolist()
        result_dict['first_rows'] = df.head().to_dict(orient='records')


        if 'GazedObject' in df.columns:
            result_dict['gazed_object_column'] = df['GazedObject'].tolist()
            result_dict['unique_gazed_objects'] = df['GazedObject'].unique().tolist()
            gazed_object_counts = df['GazedObject'].value_counts()
            total_count = gazed_object_counts.sum()
            result_dict['gazed_object_ratios'] = (gazed_object_counts / total_count).to_dict()
            gazed_object_duration = df.groupby('GazedObject')['FrameDuration'].sum().to_dict()
            result_dict['gazed_object_durations'] = gazed_object_duration
            result_dict['normalized_gazed_object_durations'] = normalized_durations.to_dict()

        result_dict['average_fps'] = average_fps
        head_and_gaze_df = get_valid_head_and_gaze_movements(df)
        result_dict['head_and_gaze_df'] = head_and_gaze_df

        stats,eye_movement_df,eye_movement_dict = detect_fixations_and_saccades(head_and_gaze_df)


        result_dict['eye_movement_statistics'] = stats
        result_dict['eye_movement_df'] = eye_movement_df
        result_dict['eye_movement_dict'] = eye_movement_dict


        # Combine eye_movement_df back into the original df
        combined_df = df.merge(
            eye_movement_df[['TimeStamp', 'MovementType', 'EyeMovementID']],
            on='TimeStamp',
            how='left'
        )

        # Fill NaN values in the combined DataFrame for non-matching rows
        combined_df['MovementType'] = combined_df['MovementType'].fillna('Invalid')
        combined_df['EyeMovementID'] = combined_df['EyeMovementID'].fillna(-1)

        result_dict['combined_df'] = combined_df

    
        # Process pupil diameter data using the new function
        pupil_data = process_pupil_diameter_data(df)
        
        result_dict['pupil_data'] = pupil_data

        return result_dict
    else:
        print(f"File {file_path} does not exist.")
        return None




def reclassify_short_fixations_as_saccades(df, min_fixation_duration=0.100):

    # Create a copy of the DataFrame to avoid modifying the original
    df_copy = df.copy()

    # Group by 'EyeMovementID' and calculate the total duration for each movement
    movement_durations = df_copy.groupby('EyeMovementID')['FrameDuration'].sum()

    # Identify fixation IDs that are shorter than the minimum duration
    short_fixation_ids = movement_durations[movement_durations < min_fixation_duration].index

    # Reclassify short fixations as saccades
    df_copy.loc[df_copy['EyeMovementID'].isin(short_fixation_ids) & (df_copy['MovementType'] == 'fixation'), 'MovementType'] = 'saccade'

    # Recalculate the IDs for fixations and saccades
    movement_types = df_copy['MovementType'].tolist()
    recalculated_ids = []
    current_id = 1
    last_type = None

    for movement_type in movement_types:
        if movement_type == last_type:
            recalculated_ids.append(current_id)
        else:
            current_id += 1
            recalculated_ids.append(current_id)
            last_type = movement_type

    df_copy['EyeMovementID'] = recalculated_ids

    return df_copy


def calculate_gaze_vectors(valid_gaze_df):
    gaze_vectors = pd.DataFrame()
    gaze_vectors['GazeVectorX'] = valid_gaze_df['CombinedGazeForwardX'] - valid_gaze_df['CombinedGazePositionX']
    gaze_vectors['GazeVectorY'] = valid_gaze_df['CombinedGazeForwardY'] - valid_gaze_df['CombinedGazePositionY']
    gaze_vectors['GazeVectorZ'] = valid_gaze_df['CombinedGazeForwardZ'] - valid_gaze_df['CombinedGazePositionZ']
    gaze_vectors = gaze_vectors.div(np.linalg.norm(gaze_vectors, axis=1), axis=0)
    return gaze_vectors


def calculate_head_direction_vectors(valid_gaze_df):
    head_vectors = pd.DataFrame()
    head_vectors['HeadVectorX'] = valid_gaze_df['HeadDirectionX'] - valid_gaze_df['HeadPositionX']
    head_vectors['HeadVectorY'] = valid_gaze_df['HeadDirectionY'] - valid_gaze_df['HeadPositionY']
    head_vectors['HeadVectorZ'] = valid_gaze_df['HeadDirectionZ'] - valid_gaze_df['HeadPositionZ']
    head_vectors = head_vectors.div(np.linalg.norm(head_vectors, axis=1), axis=0)
    return head_vectors


def calculate_angles_and_angular_velocity(vectors, timestamps):
    dot_products = (vectors.shift(1) * vectors).sum(axis=1)
    dot_products = np.clip(dot_products, -1.0, 1.0)
    angles = np.arccos(dot_products)
    angular_velocity = angles / timestamps.diff().fillna(1)
    angular_velocity_deg = np.degrees(angular_velocity)
    return angles, angular_velocity_deg



def process_outliers_fixation(movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities, tolerance=1):
    """
    Post-process the movement types to handle outliers within fixations and merge fixations with different IDs.
    Additionally, label movements that do not have valid durations as 'outlier'.

    Args:
        movement_types (list): List of movement types ('fixation', 'saccade', 'outlier', etc.) for each point.
        eye_movement_ids (list): List of movement IDs corresponding to each point.
        movement_durations (dict): Dictionary of movement durations keyed by movement ID.
        movement_amplitudes (dict): Dictionary of movement amplitudes keyed by movement ID.
        movement_velocities (dict): Dictionary of all velocity values keyed by movement ID.
        tolerance (int): Number of outliers tolerated within a fixation sequence.

    Returns:
        tuple: Updated list of movement types, updated list of eye movement IDs, updated movement durations, updated movement amplitudes, updated movement velocities.
    """
    new_movement_types = movement_types.copy()
    new_eye_movement_ids = eye_movement_ids.copy()
    new_movement_durations = movement_durations.copy()
    new_movement_amplitudes = movement_amplitudes.copy()
    new_movement_velocities = movement_velocities.copy()

    merged_ids = set()  # To track IDs that have been merged and should not be added again

    outlier_indices = [i for i, m_type in enumerate(new_movement_types) if m_type == 'outlier']
    max_outlier_id = max(new_eye_movement_ids[i] for i in outlier_indices) if outlier_indices else None

    for outlier_idx in outlier_indices:
        # Check the surrounding movement types
        prev_idx = outlier_idx - 1
        next_idx = outlier_idx + 1

        if 0 <= prev_idx < len(new_movement_types) and next_idx < len(new_movement_types):
            if (new_movement_types[prev_idx] == 'fixation_candidate' and new_movement_types[next_idx] == 'fixation_candidate') or \
                    (new_movement_types[prev_idx] == 'fixation' and new_movement_types[next_idx] == 'fixation_candidate') or \
                    (new_movement_types[prev_idx] == 'fixation_candidate' and new_movement_types[next_idx] == 'fixation') or \
                    (new_movement_types[prev_idx] == 'fixation' and new_movement_types[next_idx] == 'fixation'):

                prev_fixation_id = new_eye_movement_ids[prev_idx]
                next_fixation_id = new_eye_movement_ids[next_idx]
                outlier_id = new_eye_movement_ids[outlier_idx]  # The ID of the current outlier

                # Calculate combined duration for merged fixation
                combined_duration = (
                    new_movement_durations.get(prev_fixation_id, 0) +
                    new_movement_durations.get(next_fixation_id, 0) +
                    new_movement_durations.get(outlier_id, 0)  # Include outlier duration
                )

                if combined_duration < MAX_FIXATION_DURATION:
                    # Use the existing ID of the first fixation in merging
                    merge_id = prev_fixation_id

                    # Efficiently update all relevant points to use the merged fixation ID
                    new_movement_types[prev_idx:next_idx + 1] = ['fixation'] * (next_idx - prev_idx + 1)
                    new_eye_movement_ids[prev_idx:next_idx + 1] = [merge_id] * (next_idx - prev_idx + 1)

                    # Efficient replacement with early termination
                    encountered_control = False
                    for i in range(prev_idx, len(new_eye_movement_ids)):
                        if new_eye_movement_ids[i] == next_fixation_id:
                            new_movement_types[i] = 'fixation'
                            new_eye_movement_ids[i] = merge_id
                            encountered_control = True
                        elif encountered_control:
                            break

                    encountered_control = False
                    for i in range(0, next_idx):
                        if new_eye_movement_ids[i] == prev_fixation_id:
                            new_movement_types[i] = 'fixation'
                            new_eye_movement_ids[i] = merge_id
                            encountered_control = True
                        elif encountered_control:
                            break

                    # Update the new duration dictionary
                    new_movement_durations[merge_id] = combined_duration

                    # Calculate combined amplitude for merged fixation
                    combined_amplitude = (
                        (new_movement_amplitudes.get(prev_fixation_id, 0) * new_movement_durations.get(prev_fixation_id, 0) +
                         new_movement_amplitudes.get(next_fixation_id, 0) * new_movement_durations.get(next_fixation_id, 0) +
                         new_movement_amplitudes.get(outlier_id, 0) * new_movement_durations.get(outlier_id, 0))
                        / combined_duration
                    )
                    new_movement_amplitudes[merge_id] = combined_amplitude

                    # Combine velocity values for merged fixation
                    combined_velocities = (
                        new_movement_velocities.get(prev_fixation_id, []) +
                        new_movement_velocities.get(next_fixation_id, []) +
                        new_movement_velocities.get(outlier_id, [])
                    )
                    new_movement_velocities[merge_id] = combined_velocities

                    # Remove the old IDs from the duration, amplitude, and velocity dictionaries
                    if next_fixation_id in new_movement_durations:
                        del new_movement_durations[next_fixation_id]
                    if outlier_id in new_movement_durations:
                        del new_movement_durations[outlier_id]

                    if next_fixation_id in new_movement_amplitudes:
                        del new_movement_amplitudes[next_fixation_id]
                    if outlier_id in new_movement_amplitudes:
                        del new_movement_amplitudes[outlier_id]

                    if next_fixation_id in new_movement_velocities:
                        del new_movement_velocities[next_fixation_id]
                    if outlier_id in new_movement_velocities:
                        del new_movement_velocities[outlier_id]

                    merged_ids.update({prev_fixation_id, next_fixation_id, outlier_id})  # Mark IDs as processed

                  #  print(f"Last merged ID: {merge_id} / Max outlier ID: {max_outlier_id}")

    # Add remaining unchanged movements to the new lists and dictionary after each loop iteration
    for movement_id in set(new_eye_movement_ids):
        if movement_id not in merged_ids and movement_id not in new_movement_durations:
            # Copy original data for unchanged movements
            new_movement_durations[movement_id] = movement_durations[movement_id]
            new_movement_amplitudes[movement_id] = movement_amplitudes[movement_id]
            new_movement_velocities[movement_id] = movement_velocities[movement_id]

    return new_movement_types, new_eye_movement_ids, new_movement_durations, new_movement_amplitudes, new_movement_velocities



def process_outliers_saccade(movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities, tolerance=1):
    """
    Post-process the movement types to handle outliers within saccades and merge saccades with different IDs.
    Additionally, label movements that do not have valid durations as 'outlier'.

    Args:
        movement_types (list): List of movement types ('fixation', 'saccade', 'outlier', etc.) for each point.
        eye_movement_ids (list): List of movement IDs corresponding to each point.
        movement_durations (dict): Dictionary of movement durations keyed by movement ID.
        movement_amplitudes (dict): Dictionary of movement amplitudes keyed by movement ID.
        movement_velocities (dict): Dictionary of all velocity values keyed by movement ID.
        tolerance (int): Number of outliers tolerated within a saccade sequence.

    Returns:
        tuple: Updated list of movement types, updated list of eye movement IDs, updated movement durations, updated movement amplitudes, updated movement velocities.
    """
    new_movement_types = movement_types.copy()
    new_eye_movement_ids = eye_movement_ids.copy()
    new_movement_durations = movement_durations.copy()
    new_movement_amplitudes = movement_amplitudes.copy()
    new_movement_velocities = movement_velocities.copy()

    merged_ids = set()  # To track IDs that have been merged and should not be added again

    outlier_indices = [i for i, m_type in enumerate(new_movement_types) if m_type == 'outlier']
    max_outlier_id = max(new_eye_movement_ids[i] for i in outlier_indices) if outlier_indices else None

    for outlier_idx in outlier_indices:
        # Check the surrounding movement types
        prev_idx = outlier_idx - 1
        next_idx = outlier_idx + 1

        if 0 <= prev_idx < len(new_movement_types) and next_idx < len(new_movement_types):
            if (new_movement_types[prev_idx] == 'saccade_candidate' and new_movement_types[next_idx] == 'saccade_candidate') or \
                    (new_movement_types[prev_idx] == 'saccade' and new_movement_types[next_idx] == 'saccade_candidate') or \
                    (new_movement_types[prev_idx] == 'saccade_candidate' and new_movement_types[next_idx] == 'saccade') or \
                    (new_movement_types[prev_idx] == 'saccade' and new_movement_types[next_idx] == 'saccade'):

                prev_fixation_id = new_eye_movement_ids[prev_idx]
                next_fixation_id = new_eye_movement_ids[next_idx]
                outlier_id = new_eye_movement_ids[outlier_idx]  # The ID of the current outlier

                # Calculate combined duration for merged saccade
                combined_duration = (
                    new_movement_durations.get(prev_fixation_id, 0) +
                    new_movement_durations.get(next_fixation_id, 0) +
                    new_movement_durations.get(outlier_id, 0)  # Include outlier duration
                )

                if combined_duration < MAX_SACCADE_DURATION:
                    # Use the existing ID of the first saccade in merging
                    merge_id = prev_fixation_id  # Use the previous saccade ID

                    # Efficiently update all relevant points to use the merged saccade ID
                    new_movement_types[prev_idx:next_idx + 1] = ['saccade'] * (next_idx - prev_idx + 1)
                    new_eye_movement_ids[prev_idx:next_idx + 1] = [merge_id] * (next_idx - prev_idx + 1)

                    # Efficient replacement with early termination
                    encountered_control = False
                    for i in range(prev_idx, len(new_eye_movement_ids)):
                        if new_eye_movement_ids[i] == next_fixation_id:
                            new_movement_types[i] = 'saccade'
                            new_eye_movement_ids[i] = merge_id
                            encountered_control = True
                        elif encountered_control:
                            break

                    encountered_control = False
                    for i in range(0, next_idx):
                        if new_eye_movement_ids[i] == prev_fixation_id:
                            new_movement_types[i] = 'saccade'
                            new_eye_movement_ids[i] = merge_id
                            encountered_control = True
                        elif encountered_control:
                            break

                    # Update the new duration dictionary
                    new_movement_durations[merge_id] = combined_duration

                    # Calculate combined amplitude for merged saccade
                    combined_amplitude = (
                        (new_movement_amplitudes.get(prev_fixation_id, 0) * new_movement_durations.get(prev_fixation_id, 0) +
                         new_movement_amplitudes.get(next_fixation_id, 0) * new_movement_durations.get(next_fixation_id, 0) +
                         new_movement_amplitudes.get(outlier_id, 0) * new_movement_durations.get(outlier_id, 0))
                        / combined_duration
                    )
                    new_movement_amplitudes[merge_id] = combined_amplitude

                    # Combine velocity values for merged saccade
                    combined_velocities = (
                        new_movement_velocities.get(prev_fixation_id, []) +
                        new_movement_velocities.get(next_fixation_id, []) +
                        new_movement_velocities.get(outlier_id, [])
                    )
                    new_movement_velocities[merge_id] = combined_velocities

                    # Remove the old IDs from the duration, amplitude, and velocity dictionaries
                    if next_fixation_id in new_movement_durations:
                        del new_movement_durations[next_fixation_id]
                    if outlier_id in new_movement_durations:
                        del new_movement_durations[outlier_id]

                    if next_fixation_id in new_movement_amplitudes:
                        del new_movement_amplitudes[next_fixation_id]
                    if outlier_id in new_movement_amplitudes:
                        del new_movement_amplitudes[outlier_id]

                    if next_fixation_id in new_movement_velocities:
                        del new_movement_velocities[next_fixation_id]
                    if outlier_id in new_movement_velocities:
                        del new_movement_velocities[outlier_id]

                    merged_ids.update({prev_fixation_id, next_fixation_id, outlier_id})  # Mark IDs as processed

                 #   print(f"Last merged ID: {merge_id} / Max outlier ID: {max_outlier_id}")

    # Add remaining unchanged movements to the new lists and dictionary after each loop iteration
    for movement_id in set(new_eye_movement_ids):
        if movement_id not in merged_ids and movement_id not in new_movement_durations:
            # Copy original data for unchanged movements
            new_movement_durations[movement_id] = movement_durations[movement_id]
            new_movement_amplitudes[movement_id] = movement_amplitudes[movement_id]
            new_movement_velocities[movement_id] = movement_velocities[movement_id]

    # Now check all movements for validity based on duration thresholds
    for movement_id, duration in new_movement_durations.items():
        # Get the indices of the points with the current movement ID
        indices = [i for i, id in enumerate(new_eye_movement_ids) if id == movement_id]

        # Check if the movement type at the first index is 'fixation' or 'saccade'
        if indices:  # Ensure there are indices found
            movement_type = new_movement_types[indices[0]]  # Check the type at the first index
            if (movement_type == 'fixation' and not (MIN_FIXATION_DURATION <= duration <= MAX_FIXATION_DURATION)) or \
                    (movement_type == 'saccade' and not (MIN_SACCADE_DURATION <= duration <= MAX_SACCADE_DURATION)):
                # If a movement does not have a valid duration, mark all points with this ID as 'outlier'
                for idx in indices:
                    new_movement_types[idx] = 'outlier'

    return new_movement_types, new_eye_movement_ids, new_movement_durations, new_movement_amplitudes, new_movement_velocities


def classify_points(df):
    # Initialize lists with default values corresponding to each row in the DataFrame
    eye_movement_ids = [None] * len(df)  # Using None as a placeholder
    last_type_list = ['outlier'] * len(df)  # Default all to 'outlier'
    movement_durations = {}  # Dictionary to store the duration for each movement ID
    movement_amplitudes = {}  # Dictionary to store the average speed (amplitude) for each movement ID
    movement_velocities = {}  # Dictionary to store all velocity values for each movement ID
    current_id = 1  # Starting ID for movements
    last_type = 'saccade_candidate'  # Initialize the last movement type as 'saccade'
    movement_start_time = df['TimeStamp'].iloc[0]  # Initial timestamp for movement start

    # Initialize start index for the first movement
    movement_start_index = 0

    # Iterate over DataFrame rows
    for i in range(1, len(df)):
        gaze_velocity = df['GazeVelocity'].iloc[i]
        head_velocity = df['HeadVelocity'].iloc[i]
        timestamp = df['TimeStamp'].iloc[i]

        # Determine the current movement type based on gaze and head velocities
        if head_velocity < HEAD_VELOCITY_THRESHOLD and gaze_velocity < GAZE_VELOCITY_FIXATION_THRESHOLD:
            current_type = 'fixation_candidate'
        elif gaze_velocity > GAZE_VELOCITY_SACCADE_THRESHOLD:
            current_type = 'saccade_candidate'
        else:
            current_type = 'outlier'  # Mark as outlier if it doesn't meet any criteria

        # If movement type changes, calculate duration and update the movement_durations and velocities
        if current_type != last_type:
            movement_duration = (timestamp - movement_start_time)  # Duration in seconds

            # Check if the duration is valid for fixation or saccade
            if (last_type == 'fixation_candidate' and MIN_FIXATION_DURATION <= movement_duration <= MAX_FIXATION_DURATION) or \
                    (last_type == 'saccade_candidate' and MIN_SACCADE_DURATION <= movement_duration <= MAX_SACCADE_DURATION):
                # If valid, assign indexes to the movement ID
                for idx in range(movement_start_index, i):
                    eye_movement_ids[idx] = current_id
                    last_type_list[idx] = 'fixation' if last_type == 'fixation_candidate' else 'saccade'

                # Store the duration and amplitude of valid movement
                movement_durations[current_id] = movement_duration
                movement_amplitudes[current_id] = df['GazeVelocity'].iloc[movement_start_index:i].mean()  # Calculate mean velocity as amplitude

                # Store all velocity values for this movement
                movement_velocities[current_id] = df['GazeVelocity'].iloc[movement_start_index:i].tolist()

                current_id += 1
            else:
                for idx in range(movement_start_index, i):
                    eye_movement_ids[idx] = current_id
                    last_type_list[idx] = last_type

                # Store the duration of valid movement
                movement_durations[current_id] = movement_duration
                movement_amplitudes[current_id] = df['GazeVelocity'].iloc[movement_start_index:i].mean()  # Calculate mean velocity as amplitude

                # Store all velocity values for this movement
                movement_velocities[current_id] = df['GazeVelocity'].iloc[movement_start_index:i].tolist()

                current_id += 1

            # Update start index and time for the new movement
            movement_start_time = timestamp
            movement_start_index = i
            last_type = current_type

    # Handle the last movement after the loop, regardless of type
    final_duration = (df['TimeStamp'].iloc[-1] - movement_start_time)
    if (last_type == 'fixation_candidate' and MIN_FIXATION_DURATION <= final_duration <= MAX_FIXATION_DURATION) or \
            (last_type == 'saccade_candidate' and MIN_SACCADE_DURATION <= final_duration <= MAX_SACCADE_DURATION):
        for idx in range(movement_start_index, len(df)):
            eye_movement_ids[idx] = current_id
            last_type_list[idx] = 'fixation' if last_type == 'fixation_candidate' else 'saccade'
        movement_durations[current_id] = final_duration
        movement_amplitudes[current_id] = df['GazeVelocity'].iloc[movement_start_index:].mean()  # Calculate mean velocity as amplitude

        # Store all velocity values for this last movement
        movement_velocities[current_id] = df['GazeVelocity'].iloc[movement_start_index:].tolist()
    else:
        for idx in range(movement_start_index, len(df)):
            eye_movement_ids[idx] = current_id
            last_type_list[idx] = last_type

        # Store the duration of valid movement
        movement_durations[current_id] = final_duration
        movement_amplitudes[current_id] = df['GazeVelocity'].iloc[movement_start_index:].mean()  # Calculate mean velocity as amplitude

        # Store all velocity values for this last movement
        movement_velocities[current_id] = df['GazeVelocity'].iloc[movement_start_index:].tolist()

    # Convert lists to DataFrame to keep the indexes consistent
    movement_df = df[['TimeStamp']].copy()
    movement_df['MovementType'] = last_type_list
    movement_df['EyeMovementID'] = eye_movement_ids

    return last_type_list, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities



def get_movement_statistics(movement_types, eye_movement_ids, movement_durations, total_time):
    # Initialize dictionaries to store statistics for fixations, saccades, small saccades, long fixations, and outliers
    stats = {
        'fixation': {
            'min': None,
            'max': None,
            'mean': None,
            'count': 0,
            'percentage': 0.0,
            'time': 0.0  # Total time for fixation
        },
        'saccade': {
            'min': None,
            'max': None,
            'mean': None,
            'count': 0,
            'percentage': 0.0,
            'time': 0.0  # Total time for saccade
        },
        'saccade_candidate': {
            'min': None,
            'max': None,
            'mean': None,
            'count': 0,
            'percentage': 0.0,
            'time': 0.0  # Total time for small saccades
        },
        'other_saccades': {
            'min': None,
            'max': None,
            'mean': None,
            'count': 0,
            'percentage': 0.0,
            'time': 0.0  # Total time for long fixations
        },
        'outlier': {
            'count': 0,
            'percentage': 0.0,
            'time': 0.0  # Total time for outliers
        }
    }

    # Separate durations by movement type using a dictionary
    durations = {'fixation': [], 'saccade': [], 'saccade_candidate': [], 'other_saccades': []}
    unique_ids = {'fixation': set(), 'saccade': set(), 'saccade_candidate': set(), 'other_saccades': set()}  # To track unique IDs for each movement type

    # Accumulate durations based on unique movement ID
    for i in range(len(movement_types)):
        current_id = eye_movement_ids[i]
        current_type = movement_types[i]

        if current_type in durations and current_id != 0:  # Ignore outliers for duration
            if current_id in movement_durations and current_id not in unique_ids[current_type]:
                # Only add duration if this ID hasn't been processed yet for this type
                durations[current_type].append(movement_durations[current_id])
                stats[current_type]['time'] += movement_durations[current_id]  # Add to total time
                unique_ids[current_type].add(current_id)  # Mark this ID as processed
        elif current_type == 'outlier':
            stats['outlier']['count'] += 1

    # Calculate total duration across all unique movements
    total_duration = sum(durations['fixation']) + sum(durations['saccade']) + sum(durations['saccade_candidate']) + sum(durations['other_saccades'])

    # Function to calculate statistics
    def calculate_stats(type_durations, total_time):
        if len(type_durations) == 1:
            # If there's only one duration, min, max, and mean are the same
            duration = type_durations[0]
            return {
                'min': duration,
                'max': duration,
                'mean': duration,
                'count': 1,
                'percentage': (duration / total_duration) * 100 if total_duration > 0 else 0,
                'time_percentage': (duration / total_time) * 100 if total_time > 0 else 0
            }
        elif len(type_durations) > 1:
            # Calculate statistics normally
            total_type_duration = sum(type_durations)
            return {
                'min': np.min(type_durations),
                'max': np.max(type_durations),
                'mean': np.mean(type_durations),
                'count': len(type_durations),
                'percentage': (total_type_duration / total_duration) * 100 if total_duration > 0 else 0,
                'time_percentage': (total_type_duration / total_time) * 100 if total_time > 0 else 0
            }
        else:
            # If there are no durations, return default stats
            return {'min': None, 'max': None, 'mean': None, 'count': 0, 'percentage': 0.0, 'time_percentage': 0.0}

    # Assign calculated statistics to the respective movement types
    stats['fixation'].update(calculate_stats(durations['fixation'], total_time))
    stats['saccade'].update(calculate_stats(durations['saccade'], total_time))
    stats['saccade_candidate'].update(calculate_stats(durations['saccade_candidate'], total_time))
    stats['other_saccades'].update(calculate_stats(durations['other_saccades'], total_time))

    # Calculate percentage of outliers
    total_points = len(movement_types)
    if total_points > 0:
        stats['outlier']['percentage'] = (stats['outlier']['count'] / total_points) * 100
        stats['outlier']['time'] = total_time - (stats['fixation']['time'] + stats['saccade']['time'] + stats['saccade_candidate']['time'] + stats['other_saccades']['time'])
        stats['outlier']['time_percentage'] = (stats['outlier']['time'] / total_time) * 100 if total_time > 0 else 0

    return stats




def detect_blinks(head_gaze_df):
    # Initialize a column for Blink detection
    head_gaze_df['Blink'] = False

    # Find periods with lost gaze tracking (GazeStatus != 'VALID')
    lost_tracking_periods = head_gaze_df[head_gaze_df['GazeStatus'] != 1].index

    # Mark these periods as blinks
    head_gaze_df.loc[lost_tracking_periods, 'Blink'] = True

    return head_gaze_df

def interpolate_high_angular_velocities(angular_velocity, threshold=500):
    """
    Interpolates individual points in angular velocities if they exceed a specified threshold.

    Parameters:
    - angular_velocity (Series): Series containing gaze angular velocities.
    - threshold (float): Threshold above which velocity points will be interpolated.

    Returns:
    - interpolated_angular_velocity (Series): Interpolated gaze angular velocities.
    - percentage_changed (float): Percentage of angular velocity values that were changed.
    """

    # Create a copy of the Series to avoid modifying original data
    interpolated_angular_velocity = angular_velocity.copy()

    # Identify indices where the velocities exceed the threshold
    high_gaze_indices = interpolated_angular_velocity > threshold

    # Set these high velocity values to NaN to prepare for interpolation
    interpolated_angular_velocity[high_gaze_indices] = np.nan

    # Interpolate the NaN values linearly
    interpolated_angular_velocity.interpolate(method='linear', inplace=True)

    # Count how many values were initially changed (set to NaN)
    initial_changed_count = high_gaze_indices.sum()

    # After interpolation, replace any values still above 500 with a random value between 150 and 500
    remaining_high_gaze_indices = interpolated_angular_velocity > 500

    # Count how many values still exceed the threshold after interpolation
    remaining_changed_count = remaining_high_gaze_indices.sum()

    # Update the high values to random values between 150 and 500
    interpolated_angular_velocity[remaining_high_gaze_indices] = np.random.uniform(150, 500, size=remaining_high_gaze_indices.sum())

    # Calculate total number of changes
    total_changed_count = initial_changed_count + remaining_changed_count

    # Calculate the total number of elements in the series
    total_values = len(angular_velocity)

    # Calculate the percentage of changed values
    print(total_values)
    print(total_changed_count)
    if total_values != 0 and not np.isnan(total_values):
        if not np.isnan(total_changed_count) and np.isfinite(total_values):
            percentage_changed = (total_changed_count / total_values) * 100
        else:
            percentage_changed = 0  # or another default value
    else:
        percentage_changed = 0  # or any other fallback value
    
    #percentage_changed = (total_changed_count / total_values) * 100

    return interpolated_angular_velocity, percentage_changed

def detect_fixations_and_saccades(valid_head_gaze_df):
    # Calculate gaze and head angular velocities

    gaze_vectors = calculate_gaze_vectors(valid_head_gaze_df)
    _, gaze_angular_velocity = calculate_angles_and_angular_velocity(gaze_vectors, valid_head_gaze_df['TimeStamp'])
    head_vectors = calculate_head_direction_vectors(valid_head_gaze_df)
    _, head_angular_velocity = calculate_angles_and_angular_velocity(head_vectors, valid_head_gaze_df['TimeStamp'])


    # Initialize interpolated series
    #interpolated_gaze_velocity = gaze_angular_velocity.reindex(head_gaze_df.index).interpolate()
    #interpolated_head_velocity = head_angular_velocity.reindex(head_gaze_df.index).interpolate()

    # Apply time-based sliding window interpolation to velocity columns
    #valid_gaze_df['AngularVelocity'] = interpolate_outliers_time_window(
    #    valid_gaze_df['AngularVelocity'], valid_gaze_df['TimeStamp'], window_duration, threshold
    #)
    #valid_gaze_df['HeadVelocity'] = interpolate_outliers_time_window(
    #    valid_gaze_df['HeadVelocity'], valid_gaze_df['TimeStamp'], window_duration, threshold
    #)


    # Interpolate high angular velocities before storing them in the DataFrame
    gaze_angular_velocity, head_angular_velocity = interpolate_high_angular_velocities(
        gaze_angular_velocity, threshold=500
    )
    # Store angular and head velocities in the DataFrame
    valid_head_gaze_df['GazeVelocity'] = gaze_angular_velocity
    valid_head_gaze_df['HeadVelocity'] = head_angular_velocity


    # Classify points using updated conditions
    movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities = classify_points(
        valid_head_gaze_df)

    # Include movement_velocities in the call to process_outliers_fixation
    movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities = process_outliers_fixation(
        movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities)

   # # Step 4: Process groups of outliers and saccade candidates
   # movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities = process_outliers_saccade_candidates(
   #     movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities)

    # Include movement_velocities in the call to process_outliers_saccade
    movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities = process_outliers_saccade(
        movement_types, eye_movement_ids, movement_durations, movement_amplitudes, movement_velocities)

    # Get statistics of detected movements

    # Calculate total time from the DataFrame
    total_time = valid_head_gaze_df['TimeStamp'].iloc[-1] - valid_head_gaze_df['TimeStamp'].iloc[0]

    # Get statistics of detected movements
    stats = get_movement_statistics(movement_types, eye_movement_ids, movement_durations, total_time)

    print(stats)

    valid_head_gaze_df['FrameDuration'] = valid_head_gaze_df['TimeStamp'].diff().fillna(0)
    valid_head_gaze_df['MovementType'] = movement_types
    valid_head_gaze_df['EyeMovementID'] = eye_movement_ids



    # Create a general dictionary with unique ids as keys
    general_dict = {}

    for idx, eye_id in enumerate(eye_movement_ids):
        if eye_id not in general_dict:
            # Initialize the dictionary for a new id
            general_dict[eye_id] = {
                'MovementType': [movement_types[idx]],
                'MovementDuration': [movement_durations[eye_id]],
                'MovementAmplitude': [movement_amplitudes[eye_id]],
                'MovementVelocities': [movement_velocities[eye_id]]
            }
        else:
            # Append data for existing id
            general_dict[eye_id]['MovementType'].append(movement_types[idx])
            general_dict[eye_id]['MovementDuration'].append(movement_durations[eye_id])
            general_dict[eye_id]['MovementAmplitude'].append(movement_amplitudes[eye_id])
            general_dict[eye_id]['MovementVelocities'].append(movement_velocities[eye_id])


    return stats,valid_head_gaze_df, general_dict

# Function to detect and interpolate isolated outliers using a time-based sliding window
def interpolate_outliers_time_window(data_series, timestamps, window_duration, threshold):
    interpolated_series = data_series.copy()

    for i in range(len(data_series)):
        # Define the window range based on timestamps
        current_time = timestamps.iloc[i]
        window_start = current_time - window_duration / 2
        window_end = current_time + window_duration / 2

        # Get indices within the window
        window_indices = (timestamps >= window_start) & (timestamps <= window_end)
        window_data = data_series[window_indices]

        # Calculate median and MAD within the window
        median = window_data.median()
        mad = (window_data - median).abs().median()

        # Identify if the current point is an outlier
        if abs(data_series.iloc[i] - median) > threshold * mad:
            # Check if it's an isolated outlier (not part of a group)
            is_isolated = (
                    (i == 0 or abs(data_series.iloc[i - 1] - median) <= threshold * mad) and
                    (i == len(data_series) - 1 or abs(data_series.iloc[i + 1] - median) <= threshold * mad)
            )

            if is_isolated:
                interpolated_series.iloc[i] = np.nan

    # Interpolate NaN values (isolated outliers)
    interpolated_series.interpolate(method='linear', inplace=True)
    return interpolated_series

def read_nasa_tlx_file(filepath):
    nasatlx_result = {}

    # Open and read the file
    with open(filepath, 'r') as file:
        lines = file.readlines()

        for line in lines:
            line = line.strip()  # Remove any leading/trailing whitespace

            if line:  # Ensure the line is not empty
                # Split line into parts
                participant_case, rest = line.split(' ', 1)
                qna_topic, score = rest.split(':')
                qna, topic = qna_topic.split()

                # Extract participant ID and case number
                participant_id = participant_case.split('c')[0][1:]  # Extract digits between 'P' and 'C'
                case_number = int(participant_case.split('c')[1][0])  # Extract the number immediately after 'C'

                # Format the score
                score = float(score.replace(',', '.').strip())

                # Initialize participant entry if not already present
                if participant_id not in nasatlx_result:
                    nasatlx_result[participant_id] = {
                        'case_number': case_number,
                        'condition_tlx': {'qna': None, 'nonqna': None},
                        'topic_tlx': {'doubleslit': None, 'videogames': None}
                    }

                # Update case number if necessary (in case of multiple entries for the same participant)
                nasatlx_result[participant_id]['case_number'] = case_number

                # Assign scores to the condition_tlx and topic_tlx
                nasatlx_result[participant_id]['condition_tlx'][qna] = score
                nasatlx_result[participant_id]['topic_tlx'][topic] = score

    return nasatlx_result


def read_general_questionnaire(file_path):
    # Read the CSV file into a DataFrame
    df = pd.read_csv(file_path, delimiter=';')

    # Initialize a dictionary to store results by participant IDs
    questionnaire_results = {}

    # Iterate over each row in the DataFrame to process data
    for index, row in df.iterrows():
        # Extract participant ID from the appropriate column (assuming 'Participant ID' is the correct column)
        participant_id = row['ParticipantID'].strip().lower()  # Normalize participant ID format
        participant_id = participant_id.split('c')[0][1:]  # Extract digits between 'P' and 'C'
        if participant_id=='arti':
            participant_id='explanation'
        # Initialize participant entry if not already present
        if participant_id not in questionnaire_results:
            # Create a dictionary for this participant containing all data from the row
            participant_data = {}
            for col in df.columns:
                participant_data[col] = row[col]
            # Assign the participant data to the results dictionary
            questionnaire_results[participant_id] = participant_data

        else:
            print('no participant id')


    return questionnaire_results


"""def process_all_files_in_folder(folder_path, nasatlx_filepath,questionnaire_filepath, video_games_filepath, double_slit_filepath):
    results = {}

    # Read NASA TLX results from the file
    nasatlx_result = read_nasa_tlx_file(nasatlx_filepath)
    questionnaire_results = read_general_questionnaire(questionnaire_filepath)
    # Read Video Games MC and Double Slit MC results from their respective files
    video_games_results = read_general_questionnaire(video_games_filepath)
    double_slit_results = read_general_questionnaire(double_slit_filepath)

    # Calculate scores for each topic
    video_games_scores = calculate_scores(video_games_results, video_games_correct_answers)
    double_slit_scores = calculate_scores(double_slit_results, double_slit_correct_answers)

    count=0
    for file_name in os.listdir(folder_path):

        file_path = os.path.join(folder_path, file_name)
        if os.path.isfile(file_path) and file_name.endswith('.csv'):  # Ensure it's a CSV file
            print(f"Processing file: {file_name}")
            count=count+1
            print(count)

            # Extract file information
            file_info = parse_filename(file_name)

            # Extract participant ID, case number, and trial number
            participant_id = file_info['participant_id']
            case_number = file_info['case_number']
            trial_number = file_info['session']  # Assuming trial number corresponds to session

            # Create a unique key using participant ID, case number, and trial number
            key = (participant_id, case_number, trial_number)


            # Create a nested dictionary structure if not already present
            if key not in results:
                results[key] = {
                    'file_info': file_info,
                    'eye_tracking': {},
                    'nasatlx': {
                        'condition_tlx': None,
                        'topic_tlx': None,
                        'cognitive_load': None
                    },
                    'questionnaire_results': {},
                    'mc_quiz': {}
                }


            # Process eye-tracking data
            result_eye_tracking = process_log_file(file_path)

            # Assign the eye-tracking results if needed
            results[key]['eye_tracking'] = result_eye_tracking

            # Extract condition and topic from file_info
            condition = file_info['condition']
            topic = file_info['topic']

            # Assign condition_tlx and topic_tlx from NASA TLX results if available
            if participant_id in nasatlx_result:
                participant_data = nasatlx_result[participant_id]

                # Assign the specific condition and topic from file_info
                results[key]['nasatlx']['condition_tlx'] = condition
                results[key]['nasatlx']['topic_tlx'] = topic

                # Assign cognitive load score based on condition and topic
                cognitive_load_score = participant_data['condition_tlx'].get(condition, None)
                if cognitive_load_score is None:
                    cognitive_load_score = participant_data['topic_tlx'].get(topic, None)
                results[key]['nasatlx']['cognitive_load'] = cognitive_load_score

            # Check if the participant ID exists in questionnaire results and add it
            if participant_id in questionnaire_results:
                results[key]['questionnaire_results'] = questionnaire_results[participant_id]
            else:
                print('no participant id')

            # Assign MC scores based on topic
            if topic == 'videogames':
                if participant_id in video_games_scores:
                    results[key]['mc_quiz']['mc_familiarity'] = video_games_scores[participant_id]['familiarity']
                    results[key]['mc_quiz']['mc_quiz'] = video_games_scores[participant_id]['quiz_score']
            elif topic == 'doubleslit':
                if participant_id in double_slit_scores:
                    results[key]['mc_quiz']['mc_familiarity'] = double_slit_scores[participant_id]['familiarity']
                    results[key]['mc_quiz']['mc_quiz'] = double_slit_scores[participant_id]['quiz_score']

    return results


# Example usage
video_games_filepath = '/Users/suleymanozdel/Downloads/VRClassroomEyeTrackingData/vrClassroom/questionaries/Video Games MC_August 27, 2024_15.04.csv'
double_slit_filepath = '/Users/suleymanozdel/Downloads/VRClassroomEyeTrackingData/vrClassroom/questionaries/Double Slit MC_August 27, 2024_15.04.csv'
questionnaire_filepath= '/Users/suleymanozdel/Downloads/VRClassroomEyeTrackingData/vrClassroom/questionaries/VRClassroom General Questionnaire_August 27, 2024_11.08.csv'
nasatlx_filepath = '/Users/suleymanozdel/Downloads/VRClassroomEyeTrackingData/vrClassroom/nasaTLX/nasatlx_raw.txt'
folder_path = '/Users/suleymanozdel/Downloads/VRClassroomEyeTrackingData/EyeTrackingLogs/'
results = process_all_files_in_folder(folder_path, nasatlx_filepath, questionnaire_filepath, video_games_filepath, double_slit_filepath)"""

# Convert results dictionary to JSON-compatible format (e.g., handling non-serializable objects)
def make_json_serializable(obj):
    """
    Converts objects that are not JSON serializable to a JSON serializable format.
    """
    if isinstance(obj, pd.DataFrame):
        return obj.to_dict(orient='list')  # Convert DataFrame to dictionary format
    elif isinstance(obj, (pd.Timestamp, pd.Timedelta)):
        return str(obj)  # Convert pandas date objects to strings
    elif isinstance(obj, np.ndarray):
        return obj.tolist()  # Convert numpy arrays to lists
    else:
        raise TypeError(f"Object of type {obj.__class__.__name__} is not JSON serializable")


def convert_keys_to_strings(d):
    """
    Recursively convert all keys in a dictionary to strings.

    Args:
        d (dict): The dictionary to convert.

    Returns:
        dict: A new dictionary with all keys converted to strings.
    """
    if isinstance(d, dict):
        return {str(k): convert_keys_to_strings(v) for k, v in d.items()}
    elif isinstance(d, list):
        return [convert_keys_to_strings(i) for i in d]
    else:
        return d


# Convert results dictionary to have string keys
#results_with_string_keys = convert_keys_to_strings(results)
# Save the results dictionary to a file as JSON
"""try:
    with open('results.json', 'w') as f:
        json.dump(results_with_string_keys, f, default=make_json_serializable, indent=4)
except TypeError as e:
    print(f"Error during JSON serialization: {e}")


# Save the results dictionary to a file using Pickle
try:
    with open('results.pkl', 'wb') as f:
        pickle.dump(results, f)
except pickle.PickleError as e:
    print(f"Error during Pickle serialization: {e}")"""


app = Flask(__name__)

UPLOAD_FOLDER = "uploads"
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

@app.route('/upload', methods=['POST'])
def upload_file():
    if 'file' not in request.files:
        return "No file part", 400

    file = request.files['file']
    if file.filename == '':
        return "No selected file", 400

    file_path = os.path.join(UPLOAD_FOLDER, file.filename)
    file.save(file_path)

    # Process the file and get the results
    results_dict = process_log_file(file_path)

    if results_dict is None:
        return "Error processing file", 400
    
    # Convert DataFrames in results_dict to JSON serializable formats
    results_dict = convert_dataframes_to_json(results_dict)
    
    return jsonify(results_dict), 200

def convert_dataframes_to_json(results_dict):
    # Convert DataFrames to JSON-serializable formats
    for key, value in results_dict.items():
        if isinstance(value, pd.DataFrame):
            results_dict[key] = value.to_dict(orient='records')  # Convert DataFrame to list of dicts
        elif isinstance(value, dict):  # Recursively check nested dictionaries
            results_dict[key] = convert_dataframes_to_json(value)
    return results_dict

import pandas as pd
import numpy as np

# Constants for fixation detection
FIXATION_THRESHOLD = 100  # milliseconds
SACCADE_VELOCITY_THRESHOLD = 30  # degrees per second
COGNITIVE_OVERLOAD_THRESHOLD = 3  # Example threshold for overload

def load_eye_tracking_data(file_path):
    """Load and preprocess eye-tracking data."""
    df = pd.read_csv(file_path, delimiter=';', low_memory=False)
    df = df.dropna()  # Remove missing data
    df['TimeStamp'] = pd.to_numeric(df['TimeStamp'], errors='coerce')
    df = df.sort_values(by='TimeStamp').reset_index(drop=True)
    return df

def detect_fixations_and_saccades(df):
    """Detect fixations and saccades based on gaze stability."""
    fixations = []
    saccades = []
    current_fixation = []
    
    for i in range(1, len(df)):
        time_diff = df['TimeStamp'].iloc[i] - df['TimeStamp'].iloc[i-1]
        gaze_diff = np.linalg.norm(df[['CombinedGazePositionX', 'CombinedGazePositionY', 'CombinedGazePositionZ']].iloc[i] -
                                   df[['CombinedGazePositionX', 'CombinedGazePositionY', 'CombinedGazePositionZ']].iloc[i-1])
        
        if gaze_diff < 0.01:  # Considered a fixation if small movement
            current_fixation.append(df.iloc[i])
        else:
            if len(current_fixation) > 0:
                fixations.append(current_fixation)
                current_fixation = []
            saccades.append(df.iloc[i])
    
    fixation_ratio = len(fixations) / (len(fixations) + len(saccades) + 1e-6)
    saccade_ratio = len(saccades) / (len(fixations) + len(saccades) + 1e-6)
    return fixations, saccades, fixation_ratio, saccade_ratio

def detect_distraction(df, fixation_threshold=FIXATION_THRESHOLD):
    """Detect when a user looks away from the blackboard."""
    blackboard_fixations = df[df['GazedObject'] == 'Blackboard']
    distraction_count = 0
    
    for i in range(1, len(blackboard_fixations)):
        time_diff = blackboard_fixations['TimeStamp'].iloc[i] - blackboard_fixations['TimeStamp'].iloc[i-1]
        if time_diff < fixation_threshold:
            distraction_count += 1
    
    return distraction_count > 3  # Signal distraction if repeated quick lookaways

def cognitive_overload_detection(fixations):
    """Detect cognitive overload based on fixation duration and frequency."""
    fixation_durations = [len(fix) for fix in fixations]
    overload = any(duration > COGNITIVE_OVERLOAD_THRESHOLD for duration in fixation_durations)
    return overload

def process_eye_tracking_data(file_path):
    """Main function to process eye-tracking data."""
    df = load_eye_tracking_data(file_path)
    fixations, saccades, fixation_ratio, saccade_ratio = detect_fixations_and_saccades(df)
    distraction_detected = detect_distraction(df)
    overload_detected = cognitive_overload_detection(fixations)
    
    return {
        'Fixation Ratio': fixation_ratio,
        'Saccade Ratio': saccade_ratio,
        'Distraction Detected': distraction_detected,
        'Cognitive Overload': overload_detected
    }

# Example usage
file_path = 'ID_002_Scene__Condition_0_2024-11-05-13-01.csv'
results = process_eye_tracking_data(file_path)
print(results)


"""def localTest(file_path):
    print(file_path)
    results = process_log_file(file_path)

    print(results)
localTest("ID_002_Scene__Condition_0_2024-11-05-13-01.csv")"""

"""def process_gaze_data_from_unity(gaze_data):
    # Convert the received gaze data (which is a list of dictionaries) into a DataFrame
    df = pd.DataFrame(gaze_data)

    if df.empty:
        print("No gaze data received.")
        return None

    # Convert TimeStamp to seconds
    df['TimeStamp'] = pd.to_numeric(df['TimeStamp'], errors='coerce') / 1_000.0

    # Ensure the DataFrame is sorted by TimeStamp
    df = df.sort_values(by='TimeStamp')

    # Total duration in seconds
    total_duration_seconds = df['TimeStamp'].iloc[-1] - df['TimeStamp'].iloc[0]
    print(f"Total Duration (Seconds): {total_duration_seconds}")

    # Total duration in minutes
    total_duration_minutes = total_duration_seconds / 60.0
    print(f"Total Duration (Minutes): {total_duration_minutes}")

    # Calculate FPS
    total_frames = df.shape[0]
    average_fps = total_frames / total_duration_seconds if total_duration_seconds > 0 else 0
    print(f"Average FPS: {average_fps}")

    # Calculate Frame Duration (difference in TimeStamp)
    df['FrameDuration'] = df['TimeStamp'].diff().fillna(0)
    
    # Calculate GazeObject Duration (sum of frame durations by GazedObject)
    df['GazeObjectDuration'] = df.groupby('GazedObject')['FrameDuration'].transform('sum')
    
    # Total gaze duration
    total_gaze_duration = df['GazeObjectDuration'].sum()
    
    # Normalize gaze durations by object
    normalized_durations = df.groupby('GazedObject')['GazeObjectDuration'].sum() / total_gaze_duration

    result_dict = {
        'total_duration_minutes': total_duration_minutes,
        'column_names': df.columns.tolist(),
        'first_rows': df.head().to_dict(),
        'average_fps': average_fps
    }

    if 'GazedObject' in df.columns:
        result_dict['gazed_object_column'] = df['GazedObject'].tolist()
        result_dict['unique_gazed_objects'] = df['GazedObject'].unique().tolist()
        gazed_object_counts = df['GazedObject'].value_counts()
        total_count = gazed_object_counts.sum()
        result_dict['gazed_object_ratios'] = (gazed_object_counts / total_count).to_dict()
        gazed_object_duration = df.groupby('GazedObject')['FrameDuration'].sum().to_dict()
        result_dict['gazed_object_durations'] = gazed_object_duration
        result_dict['normalized_gazed_object_durations'] = normalized_durations.to_dict()

    # Get head and gaze movements (additional functions must be adapted for direct data)
    head_and_gaze_df = get_valid_head_and_gaze_movements(df)
    result_dict['head_and_gaze_df'] = head_and_gaze_df

    # Detect fixations and saccades
    stats, eye_movement_df, eye_movement_dict = detect_fixations_and_saccades(head_and_gaze_df)
    result_dict['eye_movement_statistics'] = stats
    result_dict['eye_movement_df'] = eye_movement_df
    result_dict['eye_movement_dict'] = eye_movement_dict

    # Combine eye_movement_df back into the original df
    combined_df = df.merge(
        eye_movement_df[['TimeStamp', 'MovementType', 'EyeMovementID']],
        on='TimeStamp',
        how='left'
    )

    # Fill NaN values for non-matching rows
    combined_df['MovementType'] = combined_df['MovementType'].fillna('Invalid')
    combined_df['EyeMovementID'] = combined_df['EyeMovementID'].fillna(-1)

    result_dict['combined_df'] = combined_df

    # Process pupil diameter data
    pupil_data = process_pupil_diameter_data(df)
    result_dict['pupil_data'] = pupil_data

    return result_dict
"""

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)  # Run on localhost

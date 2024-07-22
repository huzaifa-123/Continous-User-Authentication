import sys
import pandas as pd
import numpy as np
from sklearn.preprocessing import OneHotEncoder, MinMaxScaler
from tensorflow.keras.models import load_model
import joblib
import os

def main():
    # Suppress TensorFlow messages
    os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
    
    # Load the trained model
    model_path = 'C:/Users/huzai/Downloads/keyboard_model.h5'
    model = load_model(model_path)

    # Load the scaler and key encoder
    scaler = joblib.load('C:/Users/huzai/Downloads/scaler.pkl')
    key_encoder = joblib.load('C:/Users/huzai/Downloads/key_encoder.pkl')

    # Load mean and standard deviation of residuals
    mean_residual = joblib.load('C:/Users/huzai/Downloads/mean_residual.pkl')
    std_residual = joblib.load('C:/Users/huzai/Downloads/std_residual.pkl')

    # Define threshold for anomaly detection
    chosen_multiplier = 61
    chosen_threshold = mean_residual + (chosen_multiplier * std_residual)
  
    # Loop through command-line arguments
    for i in range(1, len(sys.argv), 2):
        # Prepare input data from command line arguments
        time = pd.to_datetime(sys.argv[i])
        key = sys.argv[i + 1]

        # Check if the key is in the encoder's categories
        if key not in key_encoder.categories_[0]:
            continue

        # Convert time to circular representation
        hour_sin = np.sin(time.hour * (2. * np.pi / 24.))
        hour_cos = np.cos(time.hour * (2. * np.pi / 24.))

        # One-hot encode the key
        key_encoded = key_encoder.transform(np.array(key).reshape(-1, 1))

        # Create the input data array
        time_features = np.array([hour_sin, hour_cos]).reshape(1, -1)
        key_features = key_encoded.flatten().reshape(1, -1)

        # Create a DataFrame for the time features to match the scaler's expected feature names
        time_features_df = pd.DataFrame(time_features, columns=['hour_sin', 'hour_cos'])

        # Normalize the time features
        time_features_normalized = scaler.transform(time_features_df)

        # Combine time and key features
        input_data = np.concatenate((time_features_normalized, key_features), axis=1)

        # Create a sequence of length 10 for prediction
        input_sequence = np.tile(input_data, (10, 1))

        # Reshape the input data for the LSTM model
        input_data_reshaped = input_sequence.reshape(1, input_sequence.shape[0], input_sequence.shape[1])

        # Predict the output
        predicted_value = model.predict(input_data_reshaped, verbose=0)[0][0]

        # Calculate residual
        residual = predicted_value

        # Determine if the residual is an anomaly
        is_anomalous = np.abs(residual - mean_residual) > chosen_threshold

        # Print the result
        print(is_anomalous)

if __name__ == '__main__':
    main()

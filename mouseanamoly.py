import sys
import pandas as pd
import joblib
from sklearn.preprocessing import StandardScaler
from pycaret.anomaly import predict_model

def main():
    # Load the model and scaler
    iforest = joblib.load('C:/Users/huzai/Downloads/isolation_forest_model.pkl')
    scaler = joblib.load('C:/Users/huzai/Downloads/mousescaler.pkl')

    # Read the input data from command line arguments
    data = {
        'Time': [sys.argv[1]],
        'X': [float(sys.argv[2])],
        'Y': [float(sys.argv[3])],
        'Action': [sys.argv[4]]
    }
    df = pd.DataFrame(data)

    # Preprocess the data similar to the training process
    df['Time'] = pd.to_datetime(df['Time'])
    df.set_index('Time', inplace=True)
    df[['X', 'Y']] = scaler.transform(df[['X', 'Y']])
    df['Action'] = pd.factorize(df['Action'])[0]

    # Predict anomalies using pycaret
    predictions = predict_model(iforest, data=df)

    # Extract anomaly scores
    anomaly_scores = predictions['Anomaly_Score']

    # Define a threshold for anomaly detection (adjust based on your needs)
    threshold = -0.12  # Example threshold, adjust as necessary

    # Determine if the latest event is an anomaly based on the threshold
    is_anomalous = anomaly_scores.iloc[0] > threshold

    # Print the result in a simplified format for easier parsing by C#
    print(is_anomalous)

if __name__ == '__main__':
    main()

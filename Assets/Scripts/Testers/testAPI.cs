using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class GestureClient : MonoBehaviour
{
    [SerializeField] public ThalmicMyo thalmicMyo;

    private int memorySize = 6;
    private int bufferSize = 50;
    private string currentGesture = "None";
    private List<string> gestureResponses = new List<string>();
    private Queue<string> previousGesture;

    void Start()
    {
        previousGesture = new Queue<string>();
        InvokeRepeating(nameof(StartPredictionRequestCoroutine), 0f, 0.004f);
    }

    void StartPredictionRequestCoroutine()
    {
        if (gestureResponses.Count >= bufferSize) { GestureProcesse(); }

        StartCoroutine(SendPredictionRequest(thalmicMyo._myoEmg));
    }

    private void GestureProcesse()
    {
        // Get the most common gesture from the responses
        string newGesture = gestureResponses.GroupBy(i => i)
                .OrderByDescending(grp => grp.Count())
                .Select(grp => grp.Key).First();
        gestureResponses.Clear();

        // Update the current gesture in memory
        if (previousGesture.Count >= memorySize) { previousGesture.Dequeue(); }
        previousGesture.Enqueue(newGesture);

        // If a gesture appears more than half the time in the memory, update the current gesture
        if (previousGesture.Count(i => i == newGesture) >= memorySize/2) { currentGesture = newGesture; }

        Debug.Log("Queue gestures: " + string.Join(", ", previousGesture));
        Debug.Log("Current gesture: " + currentGesture);

    }

    IEnumerator SendPredictionRequest(int[] emgs)
    {
        // Build the JSON using the actual EMG values
        string json = $@"{{
            ""features"": {{
                ""EMG1"": {emgs[0]},
                ""EMG2"": {emgs[1]},
                ""EMG3"": {emgs[2]},
                ""EMG4"": {emgs[3]},
                ""EMG5"": {emgs[4]},
                ""EMG6"": {emgs[5]},
                ""EMG7"": {emgs[6]},
                ""EMG8"": {emgs[7]}
            }}
        }}";

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/predict", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error: " + www.error);
            }
            else
            {
                // Parse the response JSON and log the label and probability
                string responseText = www.downloadHandler.text;
                PredictionResponse result = null;
                try
                {
                    result = JsonUtility.FromJson<PredictionResponse>(responseText);
                }
                catch
                {
                    Debug.Log("Failed to parse prediction response: " + responseText);
                }
                if (result != null)
                {
                    gestureResponses.Add(result.label);
                }
            }
        }
    }

    // Helper classes for JSON parsing
    [System.Serializable]
    public class PredictionResponse
    {
        public string label;
        public float prob;
        public List<TopKItem> topk;
    }

    [System.Serializable]
    public class TopKItem
    {
        public string label;
        public float prob;
    }
}

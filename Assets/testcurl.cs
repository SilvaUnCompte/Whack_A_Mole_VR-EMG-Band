using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class GestureClient : MonoBehaviour
{
    [SerializeField] public ThalmicMyo thalmicMyo;

    //List<int[]> emgData = new List<int[]>();
    List<string> gestureResponses = new List<string>();

    void Start()
    {
        InvokeRepeating(nameof(StartPredictionRequestCoroutine), 0f, 0.05f);
    }

    void StartPredictionRequestCoroutine()
    {
        //emgData.Add(thalmicMyo._myoEmg);
        //int emgLength = emgData[0].Length;

        //if (emgData.Count > 10)
        //{
        //    float[] averageEmg = new float[emgLength];
        //    for (int i = 0; i < emgLength; i++)
        //    {
        //        averageEmg[i] = (float)emgData.Average(emg => emg[i]);
        //    }

        //    Debug.Log("Average EMG Data: " + string.Join(", ", averageEmg));

        //    StartCoroutine(SendPredictionRequest(averageEmg));

        //    emgData.Clear(); // Clear the list after sending the request
        //}


        if (gestureResponses.Count > 10)
        {
            string t = gestureResponses.GroupBy(i => i)
                .OrderByDescending(grp => grp.Count())
                .Select(grp => grp.Key).First();

            Debug.Log(t);

            gestureResponses.Clear(); // Clear the list after logging
        }

        StartCoroutine(SendPredictionRequest(thalmicMyo._myoEmg));
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
                    //Debug.Log($"Label: {result.label}, Prob: {result.prob}");
                    //if (result.topk != null)
                    //{
                    //    foreach (TopKItem top in result.topk)
                    //    {
                    //        Debug.Log($"TopK Label: {top.label}, Prob: {top.prob}");
                    //    }
                    //}
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

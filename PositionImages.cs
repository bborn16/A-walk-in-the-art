using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PositionImages : MonoBehaviour
{
    public GameObject player;
    public Texture testTexture;
    public Camera uiCamera;

    public FileLoadType fileLoadType;

    private Dictionary<string, Texture> images;

    private Dictionary<string, Vector3> imageLocations;
    
    // Start is called before the first frame update
    async void Start()
    {     
        // get positions of images and images          
        imageLocations = new Dictionary<string, Vector3>();        
        images = new Dictionary<string, Texture>(); 
        
        // read in text file
        string path = "Assets/Resources/embedding.txt";
        StreamReader reader = new StreamReader(path); 
        string line;
        while((line = reader.ReadLine()) != null)  
        {            
            string[] lineDelimit = line.Split(' ');
            imageLocations[lineDelimit[0]] = new Vector3(((float) Convert.ToDouble(lineDelimit[1])) / 1,
             ((float) Convert.ToDouble(lineDelimit[2]) + 21) / 10 + (float) 0.5,
             ((float) Convert.ToDouble(lineDelimit[3]))/ 1);
        }
        
        reader.Close();              

        // position each image in the frame
        foreach(KeyValuePair<string, Vector3> entry in imageLocations) {
            images[entry.Key] = testTexture;
            
            GameObject canvasHolder = new GameObject();
            GameObject imageHolder = new GameObject();

            canvasHolder.name = "ImageCanvas" + entry.Key.ToString();
            canvasHolder.AddComponent<Canvas>();
            Canvas canvas = canvasHolder.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasHolder.AddComponent<CanvasScaler>();
            canvasHolder.AddComponent<OVRRaycaster>();
            canvas.worldCamera = uiCamera;           

            // Text
            imageHolder.transform.parent = canvasHolder.transform;            
            imageHolder.name = "Image" + entry.Key.ToString();
            RawImage rawImage = imageHolder.AddComponent<RawImage>();     
            
            canvas.transform.SetPositionAndRotation(entry.Value, new Quaternion());
            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.LookAt(player.transform);
            rt.sizeDelta = new Vector2(1, 1);         
            rt = rawImage.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1, 1);
            rt.SetPositionAndRotation(entry.Value, new Quaternion());

            if (fileLoadType == FileLoadType.Web) {
                rawImage.texture = await GetRemoteTexture("https://images.nga.gov/?service=asset&action=show_preview&asset="+entry.Key);
            } else {
                try {
                    rawImage.texture = Resources.Load<Texture2D>("images/"+entry.Key);
                } catch {
                    rawImage.texture = testTexture;
                }
            }
            
        }
    }

    void Update() {
        if (OVRInput.GetDown(OVRInput.Button.One)) {
            foreach(KeyValuePair<string, Vector3> entry in imageLocations) {
                RectTransform rt = GameObject.Find("ImageCanvas" + entry.Key).GetComponent<Canvas>().GetComponent<RectTransform>();
                rt.LookAt(player.transform);             
            }
        }
    }

    public enum FileLoadType {
        Local = 0,
        Web = 1
    }

    public static async Task<Texture> GetRemoteTexture ( string url ) {
        using( UnityWebRequest www = UnityWebRequestTexture.GetTexture( url ) )
        {
            //begin request:
            var asyncOp = www.SendWebRequest();

            //await until it's done: 
            while( asyncOp.isDone==false )
            {
                await Task.Delay( 1000/30 );//30 hertz
            }

            //read results:
            if( www.isNetworkError || www.isHttpError )
            {
                //log error:
                #if DEBUG
                Debug.Log( $"{ www.error }, URL:{ www.url }" );
                #endif

                //nothing to return on error:
                return null;
            }
            else
            {
                //return valid results:
                return DownloadHandlerTexture.GetContent( www );
            }
        }
    }
}

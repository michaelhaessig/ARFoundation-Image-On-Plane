using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


/*
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 */
public static class PixabayApi
{
    public static string URL = "https://pixabay.com/api/";

    // My API KEY - the api is public but please do not abuse 
    public static string KEY = "11701417-557ad1ed3c54b52741fa32e5c";

    public static PixabayImageResponse Search(string search)
    {
        // create http request
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL + $"?key={KEY}&q={search}");
        // set http method
        request.Method = "GET";
        // send request and auto close stream
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        {
            // create reader for response stream
            StreamReader reader = new StreamReader(response.GetResponseStream());
            // read the full response into a string
            string jsonResponse = reader.ReadToEnd();
            // parse string into response model object
            PixabayImageResponse pixabayImageResponse = JsonUtility.FromJson<PixabayImageResponse>(jsonResponse);
            // return result
            return pixabayImageResponse;
        }
    }
}


[Serializable]
public class PixabayImage
{
    public int id;
    public string pageURL;
    public string type;
    public string tags;
    public string previewURL;
    public int previewWidth;
    public int previewHeight;
    public string webformatURL;
    public int webformatWidth;
    public int webformatHeight;
    public string largeImageURL;
    public string fullHDURL;
    public string imageURL;
    public int imageWidth;
    public int imageHeight;
    public int imageSize;
    public int views;
    public int downloads;
    public int favorites;
    public int likes;
    public int comments;
    public int user_id;
    public string user;
    public string userImageURL;
}

[Serializable]
public class PixabayImageResponse
{
    public int total;
    public int totalHits;
    public List<PixabayImage> hits;
}

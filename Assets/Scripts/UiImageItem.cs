using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


/*
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 */
public class UiImageItem : MonoBehaviour
{
    private PixabayImage image;

    // direct mapping of child components
    public Image Img;
    public Text Text;

    public IEnumerator SetImage(PixabayImage image)
    {
        // store image ref
        this.image = image;

        Text.text = image.tags;

        // download image
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(image.previewURL);
        yield return www.SendWebRequest();

        var texture = DownloadHandlerTexture.GetContent(www);
        //set image
        Img.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
    }

    public void OpenImage()
    {
        // set the current image
        ARImageScene.CurrentImage = image;
        // load AR scene
        SceneManager.LoadScene("ARPlaceImageScene");
    }
}

using System.Collections;

using UnityEngine;
using UnityEngine.UI;

/*
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 */
public class UiMenu : MonoBehaviour
{

    public GameObject ImgList;

    public GameObject ImgItemPrefab;

    public InputField InputSearch;

    private static PixabayImageResponse SearchResponse;

    // Use this for initialization
    void Start()
    {

        // rebuild product list if we have a saved Search Response
        if (UiMenu.SearchResponse != null)
        {
            StartCoroutine(AddImagesAsync(UiMenu.SearchResponse));
        }
    }

    public void SetDebugMode(bool enabled)
    {

    }


    public void SearchImages()
    {
        var search = InputSearch.text;

        // only if search input is not empty
        if (search.Length > 0)
        {
            // clean current img list
            CleanResults();

            // StartCoroutine Async needed 
            StartCoroutine(SearchImagesAsync(search));
        }

    }

    private IEnumerator SearchImagesAsync(string text)
    {
        PixabayImageResponse pixabayImageResponse = PixabayApi.Search(text);

        if (pixabayImageResponse.total > 0)
        {
            UiMenu.SearchResponse = pixabayImageResponse;

             yield return AddImagesAsync(pixabayImageResponse);
        }
        else
        {
            // TODO show not found message
            yield break;
        }

    }

    private IEnumerator AddImagesAsync(PixabayImageResponse pixabayImageResponse)
    {
        foreach (var image in pixabayImageResponse.hits)
        {
            yield return AddImageToList(image);
        }
    }

    private void CleanResults()
    {
        foreach (Transform child in ImgList.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private IEnumerator AddImageToList(PixabayImage pixabayImage)
    {
        GameObject imagePrefabObject = Instantiate<GameObject>(ImgItemPrefab);

        UiImageItem imageItem = imagePrefabObject.GetComponent<UiImageItem>();

        yield return imageItem.SetImage(pixabayImage);

        // append to list
        // this is executed before image loading finished ..
        imageItem.transform.SetParent(ImgList.transform, false);
    }


}

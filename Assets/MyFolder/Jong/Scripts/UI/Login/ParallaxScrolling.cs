using UnityEngine;

public class ParallaxScrolling : MonoBehaviour
{
    public Transform cam;
    public RectTransform imgA, imgB;
    public float scrollSpeed = 200f;

    private float width;

    void Start()
    {
        width = imgA.rect.width;

        imgA.anchoredPosition = new Vector2(0, 0);
        imgB.anchoredPosition = new Vector2(width, 0);
    }

    void Update()
    {
        float move = scrollSpeed * Time.deltaTime;
        imgA.anchoredPosition -= new Vector2(move, 0);
        imgB.anchoredPosition -= new Vector2(move, 0);


        if (imgA.anchoredPosition.x <= -width)
        {
            imgA.anchoredPosition = new Vector2(imgB.anchoredPosition.x + width, 0);
        }

        if (imgB.anchoredPosition.x <= -width)
        {
            imgB.anchoredPosition = new Vector2(imgA.anchoredPosition.x + width, 0);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("총알 구멍")]
    public GameObject bulletHolePrefab;
    private RectTransform rectTr;

    [Header("테두리 색상 설정")]
    public Color normalColor = Color.white;   
    public Color hoverColor = Color.lightBlue;   
    public Color clickColor = Color.blue;      

    private Outline buttonOutline;
    private bool isHovering = false;

    private void Awake()
    {
        rectTr = GetComponent<RectTransform>();
        buttonOutline = GetComponent<Outline>();

        if (buttonOutline != null)
        {
            buttonOutline.effectColor = normalColor; 
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (buttonOutline != null) buttonOutline.effectColor = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (buttonOutline != null) buttonOutline.effectColor = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonOutline != null) buttonOutline.effectColor = clickColor;

        if (bulletHolePrefab == null) return;

        Vector2 localCursorPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTr, eventData.position, eventData.pressEventCamera, out localCursorPosition);

        GameObject bulletHoleGo = Instantiate(bulletHolePrefab, transform);
        RectTransform holeRect = bulletHoleGo.GetComponent<RectTransform>();

        holeRect.anchoredPosition = localCursorPosition;

        float randomAngle = Random.Range(0f, 360f);
        holeRect.localRotation = Quaternion.Euler(0, 0, randomAngle);

        Destroy(bulletHoleGo, 1f);

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonOutline != null)
        {
            buttonOutline.effectColor = isHovering ? hoverColor : normalColor;
        }
    }
}
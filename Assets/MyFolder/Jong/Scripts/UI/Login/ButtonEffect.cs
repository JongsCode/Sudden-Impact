using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("터질 파티클 프리팹")]
    public GameObject clickParticlePrefab;

    [Header("테두리 색상 설정")]
    public Color normalColor = Color.white;   
    public Color hoverColor = Color.lightBlue;   
    public Color clickColor = Color.blue;      

    private Outline buttonOutline;
    private bool isHovering = false;

    private void Awake()
    {
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

        if (clickParticlePrefab != null)
        {
            GameObject particle = Instantiate(clickParticlePrefab, transform.position, Quaternion.identity);
            Destroy(particle, 1f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonOutline != null)
        {
            buttonOutline.effectColor = isHovering ? hoverColor : normalColor;
        }
    }
}
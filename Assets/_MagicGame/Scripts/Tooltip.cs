using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[ExecuteInEditMode()]
public class Tooltip : MonoBehaviour
{
    [SerializeField] private RectTransform _canvasRt;
    [SerializeField] private TMP_Text _headerField;
    [SerializeField] private TMP_Text _contentField;
    [SerializeField] private LayoutElement _layoutElement;
	
    private RectTransform _rectTransform;
	
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (Application.isEditor)
            _layoutElement.enabled = Math.Max(_headerField.preferredWidth, _contentField.preferredWidth) >= _layoutElement.preferredWidth; ;

        if (Application.isPlaying)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
			
            transform.position = mousePos;
        }
    }

    public void SetText(string content, string header = "")
    {
        _headerField.gameObject.SetActive(!string.IsNullOrEmpty(header));

        if (_headerField.gameObject.activeSelf)
            _headerField.text = header;

        _contentField.text = content;
    }

    public void SetPivot(Vector2 pivot)
    {
        GetComponent<RectTransform>().pivot = pivot;
    }
}

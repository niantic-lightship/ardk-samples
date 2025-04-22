// Copyright 2022-2025 Niantic.

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class SwipeController : MonoBehaviour, IEndDragHandler
{

    [SerializeField] 
    private int _maxPage =2;

    int _currentPage;
    Vector3 _targetPos;
    float _dragThreshold;

    [SerializeField]
    Vector3 _pageStep;

    [SerializeField]
    RectTransform _levelPagesRect;

    [SerializeField]
    float _lerpTime;
    [SerializeField]
    Image[] _slideImage;
    [SerializeField]
    Sprite _open;
    [SerializeField]
    Sprite _close;
    // Start is called before the first frame update
    void Awake()
    {
        _currentPage = 1;
        _targetPos = _levelPagesRect.localPosition;
        _dragThreshold = Screen.width * 0.01f;
        UpdateSlider();
    }


    public void Next(){
        if(_currentPage < _maxPage){
            _currentPage++;
            _targetPos += _pageStep;
            StartCoroutine(MovePage());
        }else{
            StartCoroutine(MovePage());
        }
    }
    public void Previous(){
        if(_currentPage > 1){
            _currentPage--;
            _targetPos -= _pageStep;
            StartCoroutine(MovePage());
        }else{
            StartCoroutine(MovePage());
        }
    }
    IEnumerator MovePage(){
        var t = 0f;
        var Start = _levelPagesRect.localPosition;
        while(t < 1){
            t += Time.deltaTime / _lerpTime;
            if(t > 1)
                t = 1;
            _levelPagesRect.localPosition = Vector3.Lerp(Start, _targetPos, t);
            yield return null;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(Mathf.Abs(eventData.delta.x) > _dragThreshold){
            if(eventData.delta.x > 0){
                Previous();
            }else{
                Next();
            }
        }else{
            MovePage();
        }
        UpdateSlider();
    }

    void UpdateSlider(){
        foreach(var image in _slideImage){
            image.color = new Color(0,0,0,0.5f);
        }
        _slideImage[_currentPage-1].color = new Color(0,0,0,1);
    }
}

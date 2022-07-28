using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using DG.Tweening;

public class Joystick : MonoBehaviour
{
    [SerializeField] Image joystick;
    [SerializeField] private Canvas UICanvas;
    [SerializeField] private RectTransform NavRoot;
    [SerializeField] private float inputThreshold;
    private Camera LocationCamera;

    public enum DIRECTION { NONE, UP, RIGHT, DOWN, LEFT }
    private DIRECTION currentDirection = DIRECTION.NONE;
    public DIRECTION Direction { get { return currentDirection; } }

    private Finger fingerID = null;

    private bool disableInput;

    private bool isDragging = false;
    private float travelDist;
    private Vector2 centerPos;

    private float angle = float.NaN;
    public float Angle { get { return angle; } }

    // Start is called before the first frame update
    void Awake()
    {
        // Travel dist is how far our joystick can move from its center
        travelDist = joystick.rectTransform.sizeDelta.x;
        centerPos = joystick.transform.localPosition;
        LocationCamera = Camera.main;

        joystick.transform.DOScale(Vector3.one * 0.66f, 0.2f);
    }

    private void OnEnable()
    {
        // Subscribe to all relevant Input Manager events
        InputManager.Instance.OnStartTouch += CheckForJoystickTouch;
        InputManager.Instance.OnTouchMoved += MoveJoystick;
        InputManager.Instance.OnEndTouch += CleanUpFromInteraction;
    }

    private void OnDisable()
    {
        // Unsubscribe from all relevant Input Manager events
        InputManager.Instance.OnStartTouch -= CheckForJoystickTouch;
        InputManager.Instance.OnTouchMoved += MoveJoystick;
        InputManager.Instance.OnEndTouch -= CleanUpFromInteraction;
    }

    private void CheckForJoystickTouch(Finger finger, float time)
    {
        if (disableInput) return;

        // we should only begin getting input if the touch point
        // is contained within our sprite and we currently don't
        // have a fingerID
        if (JoystickContainsPoint(finger.screenPosition) && fingerID == null)
        {
            fingerID = finger;
            isDragging = true;
            joystick.transform.DOScale(Vector3.one, 0.2f);
        }
    }

    private void MoveJoystick(Finger finger, float time)
    {
        if (disableInput) return;

        if (isDragging && fingerID == finger)
        {
            // Translate screen interaction into a position on the UI Canvas
            // regardless of rendermode.
            Vector2 anchoredPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(   NavRoot,
                                                                        finger.screenPosition,
                                                                        UICanvas.renderMode == RenderMode.ScreenSpaceOverlay ?
                                                                        null : LocationCamera,
                                                                        out anchoredPos);

            // If the control knob is within the input threshold of the circle, a direction
            // will not be selected
            if (Vector2.Distance(anchoredPos, centerPos) < travelDist * inputThreshold)
            {
                joystick.rectTransform.anchoredPosition = anchoredPos;
                currentDirection = DIRECTION.NONE;

            }
            else if (Vector2.Distance(anchoredPos, centerPos) > travelDist * inputThreshold)
            {
                // Keeps control knob within  selection area
                if (Vector2.Distance(anchoredPos, centerPos) > travelDist)
                {
                    joystick.rectTransform.anchoredPosition = anchoredPos.normalized * travelDist;
                }
                else
                {
                    joystick.rectTransform.anchoredPosition = anchoredPos;
                }

                // Resolve angle to direction
                angle = Mathf.Atan2(anchoredPos.x, anchoredPos.y) * Mathf.Rad2Deg + 180;
                DIRECTION prevDir = currentDirection;
                if (angle < 45 || angle > 315)
                {
                    currentDirection = DIRECTION.DOWN;
                }
                else if (angle >= 45 && angle < 135)
                {
                    currentDirection = DIRECTION.LEFT;
                }
                else if (angle >= 135 && angle < 225)
                {
                    currentDirection = DIRECTION.UP;
                }
                else
                {
                    currentDirection = DIRECTION.RIGHT;
                }

                // If direction has changed, update the location text
                if (prevDir != currentDirection)
                {
                   // direction has changed
                }
            }
        }
    }

    public void DiableInput(bool b)
    {
        disableInput = b;
        CleanUpFromInteraction(fingerID, Time.time);
    }

    private void CleanUpFromInteraction(Finger finger, float time)
    {
        // We should clean up input if the OnEndTouch event belongs
        // to our finger. This way we can support multitouch
        if (fingerID == finger)
        {
            // null our fingerID to prepare for our next touch
            fingerID = null;
            isDragging = false;

            angle = float.NaN;
            joystick.transform.DOScale(Vector3.one * 0.66f, 0.2f);
            joystick.rectTransform.DOLocalMove(centerPos, 0.2f, false).SetEase(Ease.InOutSine);

            // Resets our direction to default value
            if (currentDirection != DIRECTION.NONE)
            {
                currentDirection = DIRECTION.NONE;
            }
        }
    }

    // Helper function to check if point iis inside our joystick.
    // Touch box can be more accurate if you detect if the point
    // is wihtin a circle instead of the rectangular check done
    // here
    bool JoystickContainsPoint(Vector2 point)
    {
        float width = joystick.rectTransform.sizeDelta.x;
        float height = joystick.rectTransform.sizeDelta.y;

        Vector2 pos = joystick.transform.position;

        return  pos.x - (width / 2f) < point.x &&
                point.x < pos.x + (width / 2f) &&
                pos.y - (height / 2f) < point.y &&
                point.y < pos.y + (height / 2f);                
    }
}

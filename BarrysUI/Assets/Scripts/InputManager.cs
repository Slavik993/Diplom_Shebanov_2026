using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction touchAction;
    private InputAction mouseAction;
    
    public static InputManager Instance { get; private set; }
    
    public bool IsPressed { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput != null)
        {
            touchAction = playerInput.actions.FindAction("Touch");
            mouseAction = playerInput.actions.FindAction("Click");
            
            if (touchAction != null)
            {
                touchAction.performed += OnTouchPerformed;
                touchAction.canceled += OnTouchCanceled;
            }
            
            if (mouseAction != null)
            {
                mouseAction.performed += OnMouseClicked;
                mouseAction.canceled += OnMouseCanceled;
            }
        }
    }
    
    private void OnTouchPerformed(InputAction.CallbackContext context)
    {
        IsPressed = true;
    }
    
    private void OnTouchCanceled(InputAction.CallbackContext context)
    {
        IsPressed = false;
    }
    
    private void OnMouseClicked(InputAction.CallbackContext context)
    {
        IsPressed = true;
    }
    
    private void OnMouseCanceled(InputAction.CallbackContext context)
    {
        IsPressed = false;
    }
    
    void OnDestroy()
    {
        if (touchAction != null)
        {
            touchAction.performed -= OnTouchPerformed;
            touchAction.canceled -= OnTouchCanceled;
        }
        
        if (mouseAction != null)
        {
            mouseAction.performed -= OnMouseClicked;
            mouseAction.canceled -= OnMouseCanceled;
        }
    }
    
    public Vector2 GetTouchPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }
        
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return Mouse.current.position.ReadValue();
        }
        
        return Vector2.zero;
    }
}

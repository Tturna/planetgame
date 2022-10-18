using System.Collections.Generic;
using UnityEngine;

public class PlayerController : EntityController
{
    #region Serialized Fields
    [SerializeField] private SpriteRenderer handsSr;
    [SerializeField] private Animator handsAnimator;
    
    [Header("Movement Settings")]
    [SerializeField] private float accelerationSpeed;
    [SerializeField] private float maxMoveSpeed;
    [SerializeField] private float jumpForce;
    #endregion

    #region Unserialized Components
    private SpriteRenderer _sr;
    private Animator _animator;
    #endregion

    #region Interaction Variables
    private Interactable _closestInteractable;
    private Interactable _newClosestInteractable;
    #endregion

    #region Jumping Variables
    private bool _jumping;
    private const float JumpSafetyCooldown = 0.2f;
    private float _jumpCooldownTimer;
    #endregion
    
    private Vector2 _inputVector;
    private readonly List<Interactable> _interactablesInRange = new();

    protected override void Start()
    {
        base.Start();
        
        _animator = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        HandleInteraction();
        HandleGroundCheck();
        
        if (CanControl)
        {
            HandleControls();
            
            if (_inputVector.x != 0)
            {
                _sr.flipX = _inputVector.x > 0;
                handsSr.flipX = _inputVector.x > 0;
            }
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (CanControl)
        {
            //transform.Translate(Vector3.right * (_inputVector.x * moveSpeed));
            if (Rigidbody.velocity.magnitude < maxMoveSpeed)
            {
                Rigidbody.AddForce(transform.right * (_inputVector.x * accelerationSpeed));
            }

            _animator.SetBool("running", _inputVector.x != 0);
            handsAnimator.SetBool("running", _inputVector.x != 0);
        }
    }

    private void HandleControls()
    {
        _inputVector.x = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!_jumping)
            {
                Rigidbody.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
                _jumping = true;
                _jumpCooldownTimer = JumpSafetyCooldown;
            }
        }
    }

    private void HandleGroundCheck()
    {
        // This is to make sure the raycast doesn't allow another jump as the player is moving up
        if (_jumpCooldownTimer > 0)
        {
            _jumping = true;
            _jumpCooldownTimer -= Time.deltaTime;
            return;
        }
        
        var hit = Physics2D.Raycast(transform.position, -transform.up, 0.6f, 1 << LayerMask.NameToLayer("World"));

        if (!hit) return;
        
        _jumping = false;
    }

    private void HandleInteraction()
    {
        // Interaction
        if (_interactablesInRange.Count <= 0) return;
        
        // Check for closest interactable when moving
        if (_inputVector.magnitude > 0)
        {
            // Find closest interactable
            _newClosestInteractable = GetClosestInteractable();

            // Check if the closest interactable is the same as the previous closest one
            if (_newClosestInteractable != _closestInteractable)
            {
                // Disable the prompt on the old one if there is one
                if (_closestInteractable) _closestInteractable.DisablePrompt();
                    
                _closestInteractable = _newClosestInteractable;
                _closestInteractable.PromptInteraction();
            }
        }

        if (!Input.GetKeyDown(KeyCode.F)) return;
            
        _closestInteractable.Interact(this);
        _closestInteractable.DisablePrompt();
    }

    private Interactable GetClosestInteractable()
    {
        var closest = _interactablesInRange[0];

        foreach (var interactable in _interactablesInRange)
        {
            var distToCurrent = (closest.transform.position - transform.position).magnitude;
            var distToNew = (interactable.transform.position - transform.position).magnitude;

            if (distToNew < distToCurrent)
            {
                closest = interactable;
            }
        }

        return closest;
    }

    public override void ToggleSpriteRenderer(bool state)
    {
        _sr.enabled = state;
        handsSr.enabled = state;
    }
    
    protected override void OnTriggerEnter2D(Collider2D col)
    {
        base.OnTriggerEnter2D(col);
        
        if (col.transform.parent.TryGetComponent<Interactable>(out var interactable))
        {
            _interactablesInRange.Add(interactable);
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        
        if (other.transform.parent.TryGetComponent<Interactable>(out var interactable))
        {
            interactable.DisablePrompt();

            if (interactable == _closestInteractable) _closestInteractable = null;
            
            _interactablesInRange.Remove(interactable);
        }
    }
}

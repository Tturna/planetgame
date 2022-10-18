using System;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class SpaceShipEntity : EntityController
{
    [SerializeField] private Vector2 moveSpeed;
    
    private bool _canFly;
    private EntityController _passenger;
    private Transform _oldPassengerParent;

    private Vector2 _inputVector;

    protected override void Start()
    {
        base.Start();
        
        ToggleAutoRotation(false);
        GetComponent<Interactable>().Interacted += Interaction;
    }

    private void Update()
    {
        if (!_passenger) return;

        _passenger.transform.position = transform.position;
        
        if (_passenger is PlayerController)
        {
            Controls();
        }
        else
        {
            // Do something cool if an NPC or something gets into the space ship?
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        Rigidbody.AddForce(transform.up * (_inputVector.y * moveSpeed.y));
        transform.Rotate(0,0,-_inputVector.x * moveSpeed.x, Space.Self);
    }

    private void Controls()
    {
        _inputVector.x = Input.GetAxis("Horizontal");
        _inputVector.y = Input.GetAxis("Vertical");

        if (Input.GetKey(KeyCode.Space))
        {
            _inputVector.y = 1;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            _inputVector.y = -1;
        }
    }

    private void Interaction(EntityController source)
    {
        if (_passenger && source != _passenger) return;
        
        _canFly = !_canFly;

        if (_canFly)
        {
            TogglePhysics(true);
            
            _passenger = source;
            _passenger.ToggleControl(false);
            _passenger.TogglePhysics(false);
            _passenger.ToggleSpriteRenderer(false);

            var passengerTransform = _passenger.transform;
            _oldPassengerParent = passengerTransform.parent;
            
            passengerTransform.SetParent(transform);
        }
        else
        {
            TogglePhysics(false);
            
            _passenger.ToggleControl(true);
            _passenger.TogglePhysics(true);
            _passenger.ToggleSpriteRenderer(true);
            
            _passenger.transform.SetParent(_oldPassengerParent);
            _oldPassengerParent = null;
            
            _passenger = null;
        }
    }
    
    protected override void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.TryGetComponent<Planet>(out var planet))
        {
            CurrentPlanet = planet;
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent<Planet>(out _))
        {
            CurrentPlanet = null;
        }
    }
}

using Entities;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] protected float interactRange;
    [SerializeField] private GameObject promptPrefab;
    [SerializeField] private Vector2 promptOffset;

    public bool hasToggleInteraction;

    private GameObject _promptObject;

    public delegate void OnInteractEventHandler(EntityController sourceEntity);
    public event OnInteractEventHandler Interacted;
    
    private void Start()
    {
        var interactor = new GameObject("Interactor");
        interactor.transform.SetParent(transform);
        interactor.transform.localPosition = Vector3.zero;
        interactor.layer = 7;
        
        var circleCollider = interactor.AddComponent<CircleCollider2D>();
        circleCollider.radius = interactRange;
        circleCollider.isTrigger = true;
    }

    public virtual void PromptInteraction()
    {
        if (!_promptObject)
        {
            _promptObject = Instantiate(promptPrefab, transform);
            _promptObject.transform.localPosition = promptOffset;
        }
        
        _promptObject.SetActive(true);
    }

    public virtual void DisablePrompt()
    {
        if (_promptObject)
        {
            _promptObject.SetActive(false);
        }
    }

    public virtual void Interact(EntityController source)
    {
        Debug.Log($"{source.name} interacted with {gameObject.name}.");
        OnInteracted(source);
    }

    protected virtual void OnInteracted(EntityController sourceentity)
    {
        Interacted?.Invoke(sourceentity);
    }
}
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] protected float interactRange;
    [SerializeField] private GameObject promptPrefab;
    [SerializeField] private Vector2 promptOffset;

    public bool hasToggleInteraction;

    private GameObject _promptObject;

    public delegate void OnInteractEventHandler(GameObject sourceObject);
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

    public virtual void Interact(GameObject sourceObject)
    {
        Debug.Log($"{sourceObject.name} interacted with {gameObject.name}.");
        OnInteracted(sourceObject);
    }

    protected virtual void OnInteracted(GameObject sourceObject)
    {
        Interacted?.Invoke(sourceObject);
    }
}
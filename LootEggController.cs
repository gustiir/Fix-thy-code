using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using EggCollector.Utils;
using EggCollector.Interfaces;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class LootEggController : MonoBehaviour, IPoolableObject, IPointerClickHandler
{
    public static LootEggController Instance;

    public int NumberOfClicksToOpen = 3;
    public InventoryItemController[] Items;

    [Header("Sprites")]
    public SpriteRenderer ItemSprite;
    public Animator EggAnimator;
    public Collider2D BackgroundCollider;

    [Header("Sounds")]
    public AudioClip[] CracksSounds;
    public AudioClip FanFarSounds;

    [Header("Misc")]
    public ShakeController InventoryButton;

    protected Animator anim;
    protected SpriteRenderer SpRenderer;

    protected int currentClick = 0;
    protected Action doneAction;
    protected bool opened = false;
    //TODO add new before BoxCollider2D
    protected new BoxCollider2D collider;


    // Use this for initialization
    void Start()
    {
        Instance = this;
        anim = GetComponent<Animator>();
        SpRenderer = GetComponent<SpriteRenderer>();
        collider = GetComponent<BoxCollider2D>();
        anim.SetBool("Active", false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            SetUpLootEgg(null);
        }
    }

    public void SetUpLootEgg(Action done)
    {
        currentClick = 0;
        doneAction = done;
        opened = false;
        collider.enabled = true;
        BackgroundCollider.enabled = true;
        //TODO make a hash table to use instead of all the strings (this is consistent throughout the project)
        anim.SetBool("Active", true);
        EggAnimator.SetInteger("Cracks", 0);
        EggAnimator.Play("LootEgg_Egg_Idle", 0);

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        currentClick++;
        EggAnimator.SetInteger("Cracks", currentClick);
        //TODO assert is not null (breaks game on golden egg on PC)
        EggAnimator.gameObject.GetComponent<ShakeController>().Shake();
        GlobalSoundController.Instance.PlaySound(CracksSounds[Mathf.Clamp(currentClick, 0, CracksSounds.Length - 1)]);

        if (currentClick >= NumberOfClicksToOpen)
        {
            StartCoroutine(Open());
        }
    }

    protected IEnumerator Open()
    {
        if (!opened)
        {
            opened = true;
            var selectedItem = Items[Random.Range(0, Items.Length)];
            InventoryController.Instance.AddUsage(selectedItem.Name, 1);

            ItemSprite.sprite = selectedItem.GetComponent<SpriteRenderer>().sprite;
            collider.enabled = false;
            BackgroundCollider.enabled = false;

            anim.SetTrigger("Pop");
            anim.SetBool("Active", false);
            EggAnimator.SetTrigger("Explode");
            EggAnimator.gameObject.GetComponent<ShakeController>().Shake();
            yield return new WaitForSeconds(0.4f);
            GlobalSoundController.Instance.PlaySound(FanFarSounds);
        }
    }
    /// <summary>
    /// Called on by animation.
    /// </summary>
    public void PopComplete()
    {
        if (doneAction != null)
        {
            doneAction();
        }
        InventoryButton.Shake();
    }

    /* >> IPoolableObject << */
    #region IPoolableObject
    protected ObjectPooler objectPooler;
    public virtual void ActivatedFromPool(Vector3 postion, Quaternion rotation, ObjectPooler pool)
    {
        objectPooler = pool;
        return;
    }

    public virtual void ReturnToPool()
    {
        objectPooler.ReturnToPool(this.gameObject);
    }

    public virtual void DeactivateToPool()
    {
        return;
    }


    #endregion
}

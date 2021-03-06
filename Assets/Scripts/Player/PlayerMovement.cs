using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    idle, walk, interact, attack, stagger
}

public class PlayerMovement : MonoBehaviour
{
    public float speed = 10f;

    public PlayerState currentState;

    public Signal playerHealthSignal;
    public Signal playerHit;
    public Signal decreaseMagic;

    public FloatValue currentHealth;
    public VectorValue startingPosition;
    public Inventory playerInventory;

    public GameObject arrow, magicUI;
    public Item bow;

    public SpriteRenderer recievedItemSprite;

    private Vector3 moveVector;

    private Rigidbody2D playerBody;
    private Animator playerAnimator;
 
    void Awake()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();

        currentState = PlayerState.walk;

        playerAnimator.SetFloat("moveX", 0);
        playerAnimator.SetFloat("moveY", -1);

        transform.position = startingPosition.initialValue;

        magicUI.SetActive(false);
    }

    void Update()
    {
        if(currentState == PlayerState.interact)
        {
            return;
        }

        moveVector = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f);

        if(Input.GetKeyDown(KeyCode.Space) && currentState != PlayerState.attack && currentState != PlayerState.stagger)
        {
            StartCoroutine(Attack());
        }

        else if(Input.GetKeyDown(KeyCode.M) && currentState != PlayerState.attack && currentState != PlayerState.stagger)
        {
            if(playerInventory.CheckForItem(bow))
            {
                StartCoroutine(BowAttack());
            }
        }

        if(playerInventory.CheckForItem(bow))
        {
            magicUI.SetActive(true);
        }
    }

    void FixedUpdate()
    {
        if(currentState == PlayerState.walk || currentState == PlayerState.idle)
        {
            if (moveVector != Vector3.zero)
            {
                MovePlayer();

                moveVector.x = Mathf.Round(moveVector.x);
                moveVector.y = Mathf.Round(moveVector.y);

                playerAnimator.SetFloat("moveX", moveVector.x);
                playerAnimator.SetFloat("moveY", moveVector.y);

                playerAnimator.SetBool("isWalking", true);
            }
            else
            {
                playerAnimator.SetBool("isWalking", false);
            }
        }
    }

    void MovePlayer()
    {
        moveVector.Normalize();

        playerBody.MovePosition(transform.position + moveVector * speed * Time.deltaTime);
    }

    public void RaiseItem()
    {
        if(playerInventory.currentItem != null)
        {
            if (currentState != PlayerState.interact)
            {
                playerAnimator.SetBool("itemRecieved", true);
                currentState = PlayerState.interact;
                recievedItemSprite.sprite = playerInventory.currentItem.itemSprite;
            }
            else
            {
                playerAnimator.SetBool("itemRecieved", false);
                currentState = PlayerState.idle;
                recievedItemSprite.sprite = null;
                playerInventory.currentItem = null;
            }
        }
    }

    private IEnumerator Attack()
    {
        playerAnimator.SetBool("isAttacking", true);
        currentState = PlayerState.attack;
        yield return null;

        playerAnimator.SetBool("isAttacking", false);
        yield return new WaitForSeconds(0.3f);

        if(currentState != PlayerState.interact)
        {
            currentState = PlayerState.walk;
        }
    }

    private IEnumerator BowAttack()
    {
        currentState = PlayerState.attack;
        yield return null;

        SpawnArrow();

        yield return new WaitForSeconds(0.3f);

        if (currentState != PlayerState.interact)
        {
            currentState = PlayerState.walk;
        }
    }

    private void SpawnArrow()
    {
        if(playerInventory.currentMagic > 0)
        {
            Vector2 tempVector = new Vector2(playerAnimator.GetFloat("moveX"), playerAnimator.GetFloat("moveY"));

            Arrow thisArrow = Instantiate(arrow, transform.position, Quaternion.identity).GetComponent<Arrow>();

            thisArrow.Setup(tempVector, ChooseArrowDirection());

            decreaseMagic.Raise();
        }
    }

    Vector3 ChooseArrowDirection()
    {
        float temp = Mathf.Atan2(playerAnimator.GetFloat("moveY"), playerAnimator.GetFloat("moveX")) * Mathf.Rad2Deg;
        return new Vector3(0, 0, temp);
    }

    public void PlayerCoroutine(float knockTime, float damage)
    {
        currentHealth.runtimeValue -= damage;
        playerHealthSignal.Raise();

        if (currentHealth.runtimeValue > 0)
        {
            StartCoroutine(Knock(knockTime));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    IEnumerator Knock(float knockTime)
    {
        playerHit.Raise();

        if (playerBody != null)
        {
            yield return new WaitForSeconds(knockTime);
            playerBody.velocity = Vector2.zero;

            currentState = PlayerState.idle;
        }
    }
}

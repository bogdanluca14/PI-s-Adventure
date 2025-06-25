using UnityEngine;

public class StarCollect : MonoBehaviour
{
    // Sistem de colectare a stelelor

    public Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.enabled = false;
    }

    public void PlayPop()
    {
        animator.enabled = true;
        animator.Play("StarPop", 0, 0f);
        AudioManager.instance.PlaySound("coinCollect");
    }
}

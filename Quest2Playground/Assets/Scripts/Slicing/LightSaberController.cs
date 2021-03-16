using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slicing;

[RequireComponent(typeof(AudioSource))]
public class LightSaberController : MonoBehaviour
{
    [SerializeField]
    OVRInput.Controller controller;
    [SerializeField]
    GameObject tipJoint;
    [SerializeField]
    CapsuleCollider saberCollider;
    [SerializeField]
    AudioClip onSound;
    [SerializeField]
    AudioClip offSound;
    [SerializeField]
    AudioClip humSound;

    public float saberLength = 0.9f;
    public float animTime = 1;

    private AudioSource audioSource;

    private Vector3 startTip = new Vector3();
    private Vector3 startBase = new Vector3();

    private Vector3 endTip = new Vector3();

    enum SaberState
    {
        On,
        Off,
        Animating
    }

    private SaberState saberState;

    // Start is called before the first frame update
    void Start()
    {
        tipJoint.transform.localPosition = new Vector3(0, saberLength, 0);
        saberState = SaberState.On;

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = humSound;
        audioSource.loop = true;
        audioSource.Play();

        UpdateCollider();
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if(saberState != SaberState.Animating && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
        {
            if(saberState == SaberState.On)
            {
                StartCoroutine(RetractSaber());
            }
            else
            {
                StartCoroutine(ActivateSaber());
            }
        }
    }

    void UpdateCollider()
    {
        Vector3 saberBasePos = tipJoint.transform.parent.position;

        Vector3 centerWorld = (saberBasePos + tipJoint.transform.position) / 2;
        saberCollider.center =  transform.worldToLocalMatrix.MultiplyPoint(centerWorld);
        saberCollider.height = tipJoint.transform.localPosition.y;
    }

    IEnumerator ActivateSaber()
    {
        saberState = SaberState.Animating;
        saberCollider.enabled = true;

        audioSource.PlayOneShot(onSound);

        float time = 0;

        while (true)
        {
            time += Time.deltaTime;
            float t = time / animTime;

            if (t >= 1)
            {
                tipJoint.transform.localPosition = new Vector3(0, saberLength, 0);
                UpdateCollider();
                saberState = SaberState.On;
                audioSource.Play();
                break;
            }
            else
            {
                tipJoint.transform.localPosition = new Vector3(0, saberLength * ExpInterp(t), 0);
                UpdateCollider();
                yield return new WaitForEndOfFrame();
            }
        }
    }

    IEnumerator RetractSaber()
    {
        saberState = SaberState.Animating;
        audioSource.Stop();
        audioSource.PlayOneShot(offSound);

        float time = 0;

        while (true)
        {
            time += Time.deltaTime;
            float t = time / animTime;

            if (t >= 1)
            {
                tipJoint.transform.localPosition = new Vector3(0, 0, 0);
                saberState = SaberState.Off;
                saberCollider.enabled = false;
                break;
            }
            else
            {
                tipJoint.transform.localPosition = new Vector3(0, saberLength * (1 - ExpInterp(t)), 0);
                UpdateCollider();
                yield return new WaitForEndOfFrame();
            }
        }
    }

    float ExpInterp(float t)
    {
        return -(t * t) + 2 * t;
    }

    public Transform GetTipJointTranform()
    {
        return tipJoint.transform;
    }
}

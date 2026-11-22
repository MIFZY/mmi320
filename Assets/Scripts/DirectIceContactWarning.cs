using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Collider))]
public class DirectIceContactWarning : MonoBehaviour
{
    [Header("Detection Settings")]
    public string bareFingerTag = "SeveredFinger";
    public string baggedFingerTag = "BaggedFinger";
    public float warningCooldown = 1.2f;

    [Header("Warning Message & Optional UI")]
    public string warningMessage = "Do not let the severed finger touch ice directly! Put it in a bag first.";
    public TMP_Text warningTextUI;
    public GameObject warningPanelUI;
    public float hideWarningAfterSeconds = 2.0f;

    [Header("Events")]
    public UnityEvent<string> onDirectIceWarning;

    private readonly Dictionary<EntityId, float> lastWarningTimeById = new Dictionary<EntityId, float>();
    private float hideTimer = 0f;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (warningPanelUI != null)
        {
            warningPanelUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (warningPanelUI != null && warningPanelUI.activeSelf && hideWarningAfterSeconds > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                warningPanelUI.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckDirectIceContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckDirectIceContact(other);
    }

    private bool SafeCompareTag(Component target, string tagName)
    {
        if (target == null || string.IsNullOrEmpty(tagName)) return false;
        try
        {
            return target.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private bool IsBaggedFinger(Collider other, Transform root)
    {
        if (SafeCompareTag(other, baggedFingerTag) || SafeCompareTag(root, baggedFingerTag))
            return true;

        if (root != null)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && SafeCompareTag(children[i], baggedFingerTag))
                    return true;
            }
        }

        return false;
    }

    private void CheckDirectIceContact(Collider other)
    {
        if (other == null) return;

        Rigidbody rb = other.attachedRigidbody;
        Transform targetRoot = rb != null ? rb.transform : other.transform.root;
        if (targetRoot == null) return;

        // ถ้าอยู่ในถุงแล้ว จะไม่เตือนว่าสัมผัสน้ำแข็งโดยตรง
        if (IsBaggedFinger(other, targetRoot)) return;

        bool isBareFinger = SafeCompareTag(other, bareFingerTag) || SafeCompareTag(targetRoot, bareFingerTag);
        if (!isBareFinger) return;

        EntityId id = targetRoot.GetEntityId();
        float now = Time.time;

        if (lastWarningTimeById.TryGetValue(id, out float lastTime) && (now - lastTime) < warningCooldown)
        {
            return;
        }

        lastWarningTimeById[id] = now;
        ShowWarning(warningMessage);
    }

    private void ShowWarning(string msg)
    {
        if (warningTextUI != null)
        {
            warningTextUI.text = msg;
        }

        if (warningPanelUI != null)
        {
            warningPanelUI.SetActive(true);
            hideTimer = hideWarningAfterSeconds;
        }

        onDirectIceWarning?.Invoke(msg);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public GameObject damageTextPrefab;
    public GameObject healthTextPrefab;
    public Canvas gameCanvas;

    [Header("Key and Fireball UI")]
    public GameObject fireballUI;
    public GameObject keyUI;

    [Header("Powerup UI")]
    public GameObject powerupUIPanel;
    public List<Image> powerupSlots;
    public PowerupInventory powerupInventory;

    public Dictionary<string, GameObject> powerupIcons = new Dictionary<string, GameObject>();

    public void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);

            return;
        }

        instance = this;
        
        DontDestroyOnLoad(gameObject);

        gameCanvas = FindFirstObjectByType<Canvas>();
        powerupInventory = FindFirstObjectByType<PowerupInventory>();
        
        if (powerupInventory != null)
        {
            powerupInventory.PowerupChanged.AddListener(UpdatePowerupUI);
        }

        InitializePowerupUI();
    }

    public void Start()
    {
        if(PersistentCanvas.instance != null)
        {
            gameCanvas = PersistentCanvas.instance.GetCanvas();
        }
        else
        {
            Debug.LogError("PersistentCanvas instance is null. Cannot set game canvas.");
        }
    }

    public void InitializePowerupUI()
    {
        foreach (var slot in powerupSlots)
        {
            slot.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        CharacterEvents.characterDamaged += characterTookDamage;
        CharacterEvents.characterHealed += characterHealed;
    }

    private void OnDisable()
    {
        CharacterEvents.characterDamaged -= characterTookDamage;
        CharacterEvents.characterHealed -= characterHealed;

        if (powerupInventory != null)
        {
            powerupInventory.PowerupChanged.RemoveListener(UpdatePowerupUI);
        }
    }

    public void characterTookDamage(GameObject character, int damageReceived)
    {
        Vector3 spawnPosition = Camera.main.WorldToScreenPoint(character.transform.position);
        TMP_Text tmpText = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity, gameCanvas.transform).GetComponent<TMP_Text>();
        tmpText.text = damageReceived.ToString();
    }

    public void characterHealed(GameObject character, int healthRestored)
    {
        Vector3 spawnPosition = Camera.main.WorldToScreenPoint(character.transform.position);
        TMP_Text tmpText = Instantiate(healthTextPrefab, spawnPosition, Quaternion.identity, gameCanvas.transform).GetComponent<TMP_Text>();
        tmpText.text = healthRestored.ToString();
    }

    public void UpdatePowerupUI()
    {
        int index = 0;

        foreach (var powerupEntry in powerupInventory.GetStoredPowerups())
        {
            if (index >= powerupSlots.Count)
            {
                break;
            }

            string powerupName = powerupEntry.Value.Peek().name;

            int count = powerupEntry.Value.Count;
            
            powerupSlots[index].sprite = GetPowerupSpriteFromPrefab(powerupName);
            powerupSlots[index].gameObject.SetActive(true);

            TMP_Text textComponent = powerupSlots[index].transform.GetChild(0).GetComponent<TMP_Text>();
            
            if (textComponent != null)
            {
                textComponent.text = count.ToString() + "x";
            }

            index++;
        }
        
        for (int i = index; i < powerupSlots.Count; i++)
        {
            powerupSlots[i].gameObject.SetActive(false);
        }
    }

    private Sprite GetPowerupSpriteFromPrefab(string powerupName)
    {
        GameObject powerupPrefab = Resources.Load<GameObject>($"PowerupPickups/{powerupName}");
        
        if (powerupPrefab != null)
        {
            SpriteRenderer spriteRenderer = powerupPrefab.GetComponent<SpriteRenderer>();
            
            if (spriteRenderer != null)
            {
                return spriteRenderer.sprite;
            }
        }

        return null;
    }

    public void SetFireballUI(bool isActive)
    {
        if (fireballUI != null)
        {
            fireballUI.SetActive(isActive);
        }
    }

    public void SetKeyUI(bool isActive)
    {
        if (keyUI != null)
        {
            keyUI.SetActive(isActive);
        }
    }

    public void ResetUI()
    {
        Debug.Log("UIManager: Resetting UI for new scene.");
        
        if (fireballUI != null)
        {
            fireballUI.SetActive(false);
        }

        if (keyUI != null)
        {
            keyUI.SetActive(false);
        }

        foreach (var slot in powerupSlots)
        {
            slot.gameObject.SetActive(false);
        }
    }

    public void OnExitGame(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            #if (UNITY_EDITOR || DEVELOPMENT_BUILD)
                Debug.Log(this.name + " : " + this.GetType() + " : " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            #endif

            #if (UNITY_EDITOR)
                UnityEditor.EditorApplication.isPlaying = false;
            #elif (UNITY_STANDALONE)
                Application.Quit();
            #elif (UNITY_WEBGL)
                SceneManager.LoadScene("QuitScene");
            #endif
        }
    }
}
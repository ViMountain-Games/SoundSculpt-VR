using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Gley.AllPlatformsSave; // Namespace dell'asset
using CustomInspector;

/// <summary>
/// Struttura dei dati da salvare e caricare.
/// Puoi aggiungere al suo interno altri campi se vuoi salvare più informazioni.
/// </summary>
[System.Serializable]
public class SaveData
{
    public int currentLevelIndex; // Indice del livello attualmente sbloccato/corrente
}

public class LevelManager : MonoBehaviour
{
    // Singleton per garantire che ci sia solo un'istanza di LevelManager
    // Se vuoi nascondere questo campo in Inspector, usa [HideField].
    [Title("Singleton",
           underlined: true)]
    [MessageBox("Istanza unica del LevelManager.", MessageBoxType.Info)]
    [HideField]
    public static LevelManager Instance;

    [HorizontalLine("Lista nomi delle scene", 2)]
    [MessageBox("Elenco di scene gestite (in ordine Build Settings).", MessageBoxType.Info)]
    [ForceFill(errorMessage = "Per favore assegna almeno una scena!")]
    [SerializeField]
    private List<string> sceneNames;

    [HorizontalLine("File di salvataggio", 2)]
    [Tooltip("Nome del file dove salvare i dati. Verrà salvato in Application.persistentDataPath.")]
    [SerializeField]
    private string fileName = "SaveData";

    [HorizontalLine("Eventi di caricamento scene", 2)]
    public UnityEvent OnSceneLoadingStarted;
    public UnityEvent<float> OnSceneLoadingProgress;
    public UnityEvent OnSceneLoadingEnded;

    [HorizontalLine("Progresso Caricamento")]
    [ReadOnly(DisableStyle.OnlyText)]
    public float LoadingProgress = 0f;

    [HorizontalLine("Indice del livello corrente")]
    [ReadOnly]
    public int currentLevelIndex = 0;

    // Dati di salvataggio (oggetto che rappresenta i dati salvati)
    [HideField]
    private SaveData currentSaveData;

    
    private string FullPath => Application.persistentDataPath + "/" + fileName;

    [HorizontalLine("Opzioni di salvataggio", 2)]
    [SerializeField]
    private bool encrypt = false;

    [HorizontalLine("Delay caricamento scene", 2)]
    [Tooltip("Secondi di attesa prima di avviare il caricamento della scena.")]
    [SerializeField]
    private float sceneLoadDelay = 2f;

    [HorizontalLine("Scene escluse dal salvataggio", 2)]
    [MessageBox("In queste scene, il salvataggio automatico è disattivato.", MessageBoxType.Info)]
    [SerializeField]
    private List<string> excludedAutoSaveScenes;

    [HorizontalLine("Ultimo livello salvato", 2)]
    [ReadOnly]
    public int LastSavedLevelIndex = 0;

    private void Awake()
    {
        // Configurazione del Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantiene l'oggetto tra le scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // All'avvio, carichiamo subito l'ultimo indice salvato
        LoadLastSavedLevelIndex();
    }

    #region Nuova funzione: carica solo l'indice dell'ultimo livello salvato

    /// <summary>
    /// Carica dal file il valore dell'ultimo livello salvato
    /// e lo memorizza in LastSavedLevelIndex.
    /// Non carica automaticamente la scena.
    /// </summary>
    public void LoadLastSavedLevelIndex()
    {
        Debug.Log("LoadLastSavedLevelIndex - avvio lettura file");
        // Chiamiamo l'API per caricare i dati, la callback OnLastSavedLevelLoaded imposta la variabile
        API.Load<SaveData>(FullPath, OnLastSavedLevelLoaded, encrypt);
    }

    // Callback per la lettura "solo indice"
    private void OnLastSavedLevelLoaded(SaveData data, SaveResult result, string message)
    {
        if (result == SaveResult.EmptyData || result == SaveResult.Error)
        {
            Debug.LogWarning("Nessun file di salvataggio trovato (o errore). Indice 0 di default.");
            currentSaveData = new SaveData();
            LastSavedLevelIndex = 0;
        }
        else
        {
            currentSaveData = data;
            LastSavedLevelIndex = currentSaveData.currentLevelIndex;
            Debug.Log($"[OnLastSavedLevelLoaded] Indice salvato = {LastSavedLevelIndex}");
        }
    }

    #endregion

    #region Caricamento Scene

    /// <summary>
    /// Carica il prossimo livello nella lista di scene.
    /// Incrementa l'indice del livello corrente e avvia il caricamento.
    /// Se si supera l'ultimo livello, torna al primo (ciclo infinito).
    /// Prima di iniziare il caricamento, aspetta 'sceneLoadDelay' secondi.
    /// </summary>
    public void LoadNextLevel()
    {
        Debug.Log("LoadNextLevel");

        currentLevelIndex++;

        if (currentLevelIndex >= sceneNames.Count)
        {
            // Torna al primo livello se l'indice supera l'ultimo
            currentLevelIndex = 0;
        }

        // Proviamo a salvare (se la scena non è nella lista excludedAutoSaveScenes)
        SaveProgress();

        // Avvia una coroutine che attende un delay e poi carica la scena
        StartCoroutine(LoadSceneWithDelay(sceneNames[currentLevelIndex]));
    }

    /// <summary>
    /// Carica un livello specifico dato l'indice nella lista `sceneNames`.
    /// Aggiorna l'indice corrente e avvia il caricamento.
    /// Prima di iniziare il caricamento, aspetta 'sceneLoadDelay' secondi.
    /// </summary>
    public void LoadSpecificLevel(int index)
    {
        Debug.Log("LoadSpecificLevel");

        if (index < 0 || index >= sceneNames.Count)
            return; // Controlla che l'indice sia valido

        currentLevelIndex = index;

        // Proviamo a salvare (se la scena non è nella lista excludedAutoSaveScenes)
        SaveProgress();

        // Avvia una coroutine che attende un delay e poi carica la scena
        StartCoroutine(LoadSceneWithDelay(sceneNames[currentLevelIndex]));
    }

    /// <summary>
    /// Coroutine che attende un certo delay e poi inizia il caricamento asincrono.
    /// </summary>
    private IEnumerator LoadSceneWithDelay(string sceneName)
    {
        Debug.Log("LoadSceneWithDelay");
        // Attendi il tempo specificato prima di iniziare il caricamento
        yield return new WaitForSeconds(sceneLoadDelay);

        // Avvia il caricamento asincrono vero e proprio
        yield return StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// Corrutina per caricare una scena in modo asincrono.
    /// Emette eventi per inizio, avanzamento e fine del caricamento.
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Evento: inizio caricamento
        OnSceneLoadingStarted?.Invoke();

        // Inizia il caricamento della scena
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
        asyncOp.allowSceneActivation = false; // Controllo manuale sull'attivazione della scena

        while (!asyncOp.isDone)
        {
            // Calcola la percentuale di caricamento (progress va da 0 a 0.9)
            float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            LoadingProgress = progress;

            // Evento: segnala il progresso
            OnSceneLoadingProgress?.Invoke(progress);

            // Se il caricamento è completo (>= 90%), attiva la scena
            if (asyncOp.progress >= 0.9f)
            {
                asyncOp.allowSceneActivation = true;
            }

            yield return null; // Aspetta il frame successivo
        }

        // Evento: fine caricamento
        OnSceneLoadingEnded?.Invoke();
    }

    #endregion

    #region Salvataggio con AllPlatformsSave

    /// <summary>
    /// Salva i dati correnti (ad esempio, l'indice del livello) su disco
    /// SOLO se la scena attuale non è esclusa e il currentLevelIndex è maggiore di quello già salvato.
    /// </summary>
    public void SaveProgress()
    {
        // Se la scena corrente è nella lista delle escluse, esci subito
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (excludedAutoSaveScenes.Contains(activeSceneName))
        {
            Debug.Log($"[SaveProgress] Scena '{activeSceneName}' è esclusa dal salvataggio automatico, non salvo.");
            return;
        }

        if (currentSaveData == null)
            currentSaveData = new SaveData();

        // Se stiamo andando a un livello più avanzato rispetto a quello salvato, aggiorniamo il file
        if (currentLevelIndex > currentSaveData.currentLevelIndex)
        {
            Debug.Log($"[SaveProgress] Nuovo livello (index {currentLevelIndex}) > salvato (index {currentSaveData.currentLevelIndex}), salvo su disco.");
            currentSaveData.currentLevelIndex = currentLevelIndex;
            API.Save(currentSaveData, FullPath, DataWasSaved, encrypt);
        }
        else
        {
            Debug.Log($"[SaveProgress] Indice {currentLevelIndex} <= salvato {currentSaveData.currentLevelIndex}, non aggiorno il file.");
        }
    }

    /// <summary>
    /// Callback eseguita dopo il salvataggio.
    /// Controlla l'esito e segnala eventuali errori.
    /// </summary>
    private void DataWasSaved(SaveResult result, string message)
    {
        if (result == SaveResult.Error)
        {
            Debug.LogError("Errore durante il salvataggio: " + message);
        }
        else
        {
            Debug.Log("Dati salvati correttamente.");
        }
    }

    /// <summary>
    /// (Opzionale) Carica i dati salvati da disco e sposta la scena.
    /// Puoi richiamarlo manualmente se vuoi implementare un pulsante "Carica Partita".
    /// </summary>
    public void LoadProgress()
    {
        Debug.Log("LoadProgress - (opzionale, non è chiamato di default)");
        API.Load<SaveData>(FullPath, DataWasLoaded, encrypt);
    }

    /// <summary>
    /// (Opzionale) Callback eseguita dopo il caricamento dei dati (nel caso usassi LoadProgress).
    /// Se non esistono dati, crea un nuovo salvataggio.
    /// </summary>
    private void DataWasLoaded(SaveData data, SaveResult result, string message)
    {
        if (result == SaveResult.EmptyData || result == SaveResult.Error)
        {
            currentSaveData = new SaveData();
            currentLevelIndex = 0;
            Debug.LogWarning("LoadProgress -> file inesistente/errore, creato nuovo salvataggio con indice 0.");
            // Salvataggio iniziale di base
            API.Save(currentSaveData, FullPath, null, encrypt);
        }
        else
        {
            currentSaveData = data;
            currentLevelIndex = currentSaveData.currentLevelIndex;
            Debug.Log($"LoadProgress -> caricati i dati, currentLevelIndex={currentLevelIndex}");
        }

        // Se vuoi spostarti alla scena salvata, puoi farlo qui, ad esempio:
        // StartCoroutine(LoadSceneWithDelay(sceneNames[currentLevelIndex]));
    }

    /// <summary>
    /// Resetta completamente il progresso del gioco.
    /// Cancella il file di salvataggio e reimposta i valori iniziali.
    /// </summary>
    public void ResetProgress()
    {
        Debug.Log("ResetProgress");
        // Cancella il file di salvataggio
        API.ClearFile(FullPath);

        // Opzionale: cancella tutti i file dal persistentDataPath
        // API.ClearAllData(Application.persistentDataPath);

        // Reimposta i valori iniziali
        currentSaveData = new SaveData();
        currentLevelIndex = 0;
        LastSavedLevelIndex = 0;
        LoadingProgress = 0f;
    }

    #endregion
}

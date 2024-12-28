using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools; 
// e racchiudere la classe nel namespace MoreMountains.Tools

public class AudioAnalyzerActivator : MonoBehaviour
{
    [Header("Lista Analyzers da controllare (abilita/disabilita)")]
    public List<MMAudioAnalyzer> audioAnalyzers = new List<MMAudioAnalyzer>();

    [Header("Analyzers da disattivare all'avvio")]
    public List<MMAudioAnalyzer> analyzersToDeactivateOnStart = new List<MMAudioAnalyzer>();

    private void Start()
    {
        // Disattiva tutti gli Analyzers presenti in questa lista non appena parte la scena
        foreach (MMAudioAnalyzer analyzer in analyzersToDeactivateOnStart)
        {
            if (analyzer != null)
            {
                analyzer.enabled = false;
            }
        }
    }

    /// <summary>
    /// Attiva l'audio analyzer all'indice specificato nella lista "audioAnalyzers"
    /// </summary>
    public void EnableAnalyzer(int index)
    {
        if (index >= 0 && index < audioAnalyzers.Count)
        {
            audioAnalyzers[index].enabled = true;
        }
        else
        {
            Debug.LogWarning($"Indice {index} non valido per la lista audioAnalyzers.");
        }
    }

    /// <summary>
    /// Disattiva l'audio analyzer all'indice specificato nella lista "audioAnalyzers"
    /// </summary>
    public void DisableAnalyzer(int index)
    {
        if (index >= 0 && index < audioAnalyzers.Count)
        {
            audioAnalyzers[index].enabled = false;
        }
        else
        {
            Debug.LogWarning($"Indice {index} non valido per la lista audioAnalyzers.");
        }
    }
}

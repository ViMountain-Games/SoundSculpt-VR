using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;  // Necessario per usare EditorUtility.SetDirty
using CustomInspector;

public class NotePicker : MonoBehaviour
{
    [Header("Lista di tag validi")]
    public List<string> validTags;

    [Header("GameObject selezionato")]
    [ReadOnly] public GameObject selectedObject;

    [Header("Delay in secondi")]
    public float delay = 1.0f;

    private Grid3DGenerator gridGenerator;

    private void Start()
    {
        // Ottieni il riferimento all'istanza di Grid3DGenerator
        gridGenerator = Grid3DGenerator.Instance;
        if (gridGenerator == null)
        {
            Debug.LogError("Nessuna istanza di Grid3DGenerator trovata!");
        }
    }

    public void AssignChildWithValidTag()
    {
        StartCoroutine(AssignChildWithDelay());
    }

    private IEnumerator AssignChildWithDelay()
    {
        yield return new WaitForSeconds(delay);

        selectedObject = null;
        foreach (Transform child in transform)
        {
            if (validTags.Contains(child.tag))
            {
                selectedObject = child.gameObject;
                break;
            }
        }

        if (selectedObject != null && gridGenerator != null)
        {
            int x = Mathf.FloorToInt((transform.position.x - gridGenerator.transform.position.x) / gridGenerator.cellSize);
            int y = Mathf.FloorToInt((transform.position.y - gridGenerator.transform.position.y) / gridGenerator.cellSize);
            int z = Mathf.FloorToInt((transform.position.z - gridGenerator.transform.position.z) / gridGenerator.cellSize);

            if (x >= 0 && x < gridGenerator.gridSizeX && y >= 0 && y < gridGenerator.gridSizeY && z >= 0 && z < gridGenerator.gridSizeZ)
            {
                gridGenerator.UpdateGridMatrix(x, y, z, selectedObject);

                // Debug: Stampa conferma di assegnazione
                //Debug.Log($"Object {selectedObject.name} assigned to grid at ({x}, {y}, {z}).");
            }
            else
            {
                Debug.LogError($"Indices out of bounds: ({x}, {y}, {z}). Object not assigned.");
            }
        }
        else
        {
            Debug.LogError("SelectedObject is invalid or Grid3DGenerator is not assigned.");
        }

        EditorUtility.SetDirty(this);
    }


    public void RemoveAssignedObject()
    {
        // Rimuovi l'oggetto dalla matrice
        if (gridGenerator != null && selectedObject != null)
        {
            int x = Mathf.FloorToInt((transform.position.x - gridGenerator.transform.position.x) / gridGenerator.cellSize);
            int y = Mathf.FloorToInt((transform.position.y - gridGenerator.transform.position.y) / gridGenerator.cellSize);
            int z = Mathf.FloorToInt((transform.position.z - gridGenerator.transform.position.z) / gridGenerator.cellSize);

            // Verifica che gli indici siano validi prima di rimuovere dalla matrice
            if (x >= 0 && x < gridGenerator.gridSizeX && y >= 0 && y < gridGenerator.gridSizeY && z >= 0 && z < gridGenerator.gridSizeZ)
            {
                gridGenerator.UpdateGridMatrix(x, y, z, null); // Imposta a null la cella nella matrice
            }
        }

        selectedObject = null;
        EditorUtility.SetDirty(this); // Forza l'aggiornamento dell'Inspector
    }
}

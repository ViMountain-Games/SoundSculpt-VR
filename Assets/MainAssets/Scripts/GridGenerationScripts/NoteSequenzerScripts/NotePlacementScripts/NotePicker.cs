using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using CustomInspector;
using Autohand;

namespace GridGen
{
    public class NotePicker : MonoBehaviour
    {
        [Header("Lista di tag validi")]
        public List<string> validTags;

        [Header("GameObject selezionato")]
        [ReadOnly]
        public GameObject selectedObject;

        [Header("Delay in secondi")]
        public float delay = 1.0f;

        [Header("Grid Generator (assegnare da Inspector o tramite instanziazione)")]
        public Grid3DGenerator gridGenerator;

        private void Start()
        {
            if (!gridGenerator)
            {
                Debug.LogWarning($"[NotePicker on {name}] gridGenerator non assegnato in Inspector/istanziazione.");
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
                // Calcoliamo gli indici di cella
                int x = Mathf.FloorToInt((transform.position.x - gridGenerator.transform.position.x) / gridGenerator.cellSize.value);
                int y = Mathf.FloorToInt((transform.position.y - gridGenerator.transform.position.y) / gridGenerator.cellSize.value);
                int z = Mathf.FloorToInt((transform.position.z - gridGenerator.transform.position.z) / gridGenerator.cellSize.value);

                if (x >= 0 && x < gridGenerator.gridSizeX &&
                    y >= 0 && y < gridGenerator.gridSizeY &&
                    z >= 0 && z < gridGenerator.gridSizeZ)
                {
                    gridGenerator.UpdateGridMatrix(x, y, z, selectedObject);

                    // Se l'oggetto selezionato è una Nota
                    Note noteComponent = selectedObject.GetComponent<Note>();
                    if (noteComponent != null)
                    {
                        noteComponent.SetGridPosition(x, y, z);
                        // Attiviamo il bool di "piazzato"
                        noteComponent.isPlaced = true;
                    }

                    // [NUOVA MODIFICA] Se l'oggetto ha un MusicScaleGenerator, assegniamogli lo stesso gridGenerator
                    MusicScaleGenerator ms = selectedObject.GetComponent<MusicScaleGenerator>();
                    if (ms != null)
                    {
                        ms.gridGenerator = gridGenerator;
                    }
                }
                else
                {
                    Debug.LogError($"Indices out of bounds: ({x}, {y}, {z}). [NotePicker on {name}]");
                }
            }
            else
            {
                Debug.LogError($"[NotePicker on {name}] No valid child or missing gridGenerator.");
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveAssignedObject()
        {
            if (gridGenerator != null && selectedObject != null)
            {
                int x = Mathf.FloorToInt((transform.position.x - gridGenerator.transform.position.x) / gridGenerator.cellSize.value);
                int y = Mathf.FloorToInt((transform.position.y - gridGenerator.transform.position.y) / gridGenerator.cellSize.value);
                int z = Mathf.FloorToInt((transform.position.z - gridGenerator.transform.position.z) / gridGenerator.cellSize.value);

                if (x >= 0 && x < gridGenerator.gridSizeX &&
                    y >= 0 && y < gridGenerator.gridSizeY &&
                    z >= 0 && z < gridGenerator.gridSizeZ)
                {
                    gridGenerator.UpdateGridMatrix(x, y, z, null);
                }
            }

            // Se l'oggetto rimosso era una Note, disattiviamo il bool "isPlaced"
            if (selectedObject != null)
            {
                Note noteComponent = selectedObject.GetComponent<Note>();
                if (noteComponent != null)
                {
                    noteComponent.isPlaced = false;
                }
            }

            selectedObject = null;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }
}

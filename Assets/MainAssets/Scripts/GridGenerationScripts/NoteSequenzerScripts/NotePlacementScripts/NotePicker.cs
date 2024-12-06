using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // Questo rimane per il codice editor
#endif
using CustomInspector;

namespace GridGen
{
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
                int x = Mathf.FloorToInt((transform.position.x - gridGenerator.transform.position.x) / gridGenerator.cellSize.value);
                int y = Mathf.FloorToInt((transform.position.y - gridGenerator.transform.position.y) / gridGenerator.cellSize.value);
                int z = Mathf.FloorToInt((transform.position.z - gridGenerator.transform.position.z) / gridGenerator.cellSize.value);

                if (x >= 0 && x < gridGenerator.gridSizeX && y >= 0 && y < gridGenerator.gridSizeY && z >= 0 && z < gridGenerator.gridSizeZ)
                {
                    gridGenerator.UpdateGridMatrix(x, y, z, selectedObject);

                    Note noteComponent = selectedObject.GetComponent<Note>();
                    if (noteComponent != null)
                    {
                        noteComponent.SetGridPosition(x, y, z);
                    }
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

                if (x >= 0 && x < gridGenerator.gridSizeX && y >= 0 && y < gridGenerator.gridSizeY && z >= 0 && z < gridGenerator.gridSizeZ)
                {
                    gridGenerator.UpdateGridMatrix(x, y, z, null);
                }
            }

            selectedObject = null;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }
}
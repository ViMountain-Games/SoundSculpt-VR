using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using GridGen;
using CustomInspector;

namespace Autohand
{
    public enum PlacePointNameType
    {
        name,
        tag
    }

    public enum PlacePointShape
    {
        Sphere,
        Box
    }

    public delegate void PlacePointEvent(PlacePoint point, Grabbable grabbable);
    [Serializable]
    public class UnityPlacePointEvent : UnityEvent<PlacePoint, Grabbable> { }

    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/place-point")]
    [DefaultExecutionOrder(1000)]
    public class PlacePoint : MonoBehaviour, IGrabbableEvents
    {
        [AutoHeader("Place Point")]
        public bool ignoreMe;

        private NotePicker notePicker;
        private Grid3DGenerator gridGenerator;

        [AutoSmallHeader("Place Settings")]
        public bool showPlaceSettings = true;
        public Grabbable startPlaced;
        public Transform placedOffset;

        public PlacePointShape shapeType = PlacePointShape.Sphere;
        public float placeRadius = 0.1f;
        public Vector3 placeSize = new Vector3(0.1f, 0.1f, 0.1f);
        public Vector3 shapeOffset;

        public bool grabbablePlacePoint = true;
        public bool forcePlace = false;
        public bool forceHandRelease = true;
        public bool parentOnPlace = true;

        public bool matchPosition = true;
        public bool matchRotation = true;

        public bool resizeOnPlace = false;
        public float resizeOffset = -0.02f;

        public bool disableRigidbodyOnPlace = false;
        public bool disableGrabOnPlace = false;
        public bool disablePlacePointOnPlace = false;
        public bool destroyObjectOnPlace = false;

        public bool makePlacedKinematic = true;
        public Rigidbody placedJointLink;
        public float jointBreakForce = 1000;

        [AutoSmallHeader("Place Requirements")]
        public bool showPlaceRequirements = true;

        public bool heldPlaceOnly = false;
        public PlacePointNameType nameCompareType;
        public string[] placeNames;
        public string[] blacklistNames;
        public List<Grabbable> onlyAllows;
        public List<Grabbable> dontAllows;
        public LayerMask placeLayers;

        [AutoToggleHeader("Show Events")]
        public bool showEvents = true;
        [NaughtyAttributes.ShowIf("showEvents")]
        public UnityPlacePointEvent OnPlace;
        [NaughtyAttributes.ShowIf("showEvents")]
        public UnityPlacePointEvent OnRemove;
        [NaughtyAttributes.ShowIf("showEvents")]
        public UnityPlacePointEvent OnHighlight;
        [NaughtyAttributes.ShowIf("showEvents")]
        public UnityPlacePointEvent OnStopHighlight;

        public PlacePointEvent OnPlaceEvent;
        public PlacePointEvent OnRemoveEvent;
        public PlacePointEvent OnHighlightEvent;
        public PlacePointEvent OnStopHighlightEvent;

        public Grabbable highlightingObj { get; protected set; } = null;
        public Grabbable placedObject { get; protected set; } = null;
        public Grabbable lastPlacedObject { get; protected set; } = null;

        internal Grabbable parentGrabbable;

        protected FixedJoint joint = null;
        protected float lastPlacedTime;
        protected CollisionDetectionMode placedObjDetectionMode;
        protected float tickRate = 0.05f;

        Coroutine checkRoutine;
        Collider[] collidersNonAlloc = new Collider[50];
        Vector3 lastPlacePosition = Vector3.zero;
        bool wasInstantGrab = false;
        Vector3 prefitScale;
        protected bool placingFrame;

        private int placedX, placedY, placedZ;

        protected virtual void Awake()
        {
            if (placedOffset == null)
                placedOffset = transform;

            if (placeLayers == 0)
                placeLayers = LayerMask.GetMask(Hand.grabbableLayerNameDefault);

            gridGenerator = Grid3DGenerator.Instance;
        }

        protected virtual void OnEnable()
        {
            if (checkRoutine == null)
                checkRoutine = StartCoroutine(CheckPlaceObjectLoop());
        }

        protected virtual void OnDisable()
        {
            if (checkRoutine != null)
            {
                StopCoroutine(checkRoutine);
                checkRoutine = null;
            }
            StopHighlight();
        }

        protected virtual void CheckInvalidSettings()
        {
            if (parentGrabbable && !disableRigidbodyOnPlace && parentOnPlace)
            {
                Debug.LogWarning("Place Points placed under a grabbable cannot support parenting other rigidbody grabbables, disable rigibody on place is being enabled", this);
                disableRigidbodyOnPlace = true;
                makePlacedKinematic = false;
            }
        }

        IEnumerator LateStart()
        {
            bool waitForChildPointPlacement = false;
            int maxWaitFrames = 10;
            while (true)
            {
                yield return new WaitForFixedUpdate();

                if (startPlaced != null && startPlaced.childPlacePoints.Count > 0)
                {
                    foreach (var childPoint in startPlaced.childPlacePoints)
                    {
                        if (childPoint.startPlaced != null && childPoint.placedObject == null)
                        {
                            if (parentGrabbable == null || childPoint.startPlaced != parentGrabbable)
                            {
                                waitForChildPointPlacement = true;
                                break;
                            }
                        }
                    }
                }

                if (waitForChildPointPlacement && maxWaitFrames > 0)
                {
                    maxWaitFrames--;
                    continue;
                }
                else
                {
                    if (startPlaced != null && startPlaced.childPlacePoints.Count > 0)
                        SetStartPlaced();

                    break;
                }

            }
        }

        protected virtual void SetStartPlaced()
        {
            if (startPlaced != null)
            {
                if (startPlaced.gameObject.scene.IsValid())
                {
                    Highlight(startPlaced);
                    Place(startPlaced);
                }
                else
                {
                    var instance = GameObject.Instantiate(startPlaced);
                    instance.transform.position = placedOffset.position;
                    instance.transform.rotation = placedOffset.rotation;
                    Highlight(instance);
                    Place(instance);
                }
            }
        }

        public Grabbable GetPlacedObject()
        {
            return placedObject;
        }

        public virtual bool CanPlace(Grabbable placeObj, bool checkRoot = true)
        {
            if (gridGenerator == null)
                return false;

            if (checkRoot && CanPlace(placeObj.rootGrabbable, false))
                return true;

            if (placedObject != null)
            {
                return false;
            }

            if (!placeObj.parentOnGrab && parentGrabbable != null)
            {
                return false;
            }

            if (heldPlaceOnly && placeObj.HeldCount() == 0)
            {
                return false;
            }

            if (onlyAllows.Count > 0 && !onlyAllows.Contains(placeObj))
            {
                return false;
            }

            if (dontAllows.Count > 0 && dontAllows.Contains(placeObj))
            {
                return false;
            }

            if (blacklistNames.Length > 0)
            {
                foreach (var badName in blacklistNames)
                {
                    if (nameCompareType == PlacePointNameType.name && placeObj.name.Contains(badName))
                        return false;
                    if (nameCompareType == PlacePointNameType.tag && placeObj.CompareTag(badName))
                        return false;
                }
            }

            if (placeNames.Length > 0)
            {
                bool allowedByName = false;
                foreach (var placeName in placeNames)
                {
                    if (nameCompareType == PlacePointNameType.name && placeObj.name.Contains(placeName))
                        allowedByName = true;
                    if (nameCompareType == PlacePointNameType.tag && placeObj.CompareTag(placeName))
                        allowedByName = true;
                }
                if (!allowedByName)
                    return false;
            }

            // Controllo dello spazio sulla griglia
            Note noteComponent = placeObj.GetComponent<Note>();
            if (noteComponent != null && gridGenerator != null)
            {
                NoteData.NoteDuration duration = noteComponent.noteData.duration;
                int x = Mathf.FloorToInt((placedOffset.position.x - gridGenerator.transform.position.x) / gridGenerator.cellSize.value);
                int y = Mathf.FloorToInt((placedOffset.position.y - gridGenerator.transform.position.y) / gridGenerator.cellSize.value);
                int z = Mathf.FloorToInt((placedOffset.position.z - gridGenerator.transform.position.z) / gridGenerator.cellSize.value);

                if (!gridGenerator.CanPlaceNote(x, y, z, duration))
                {
                    return false;
                }
            }

            return true;
        }

        public virtual void TryPlace(Grabbable placeObj)
        {
            if (CanPlace(placeObj))
                Place(placeObj);
        }

        public virtual void Place(Grabbable placeObj)
        {
            if (placedObject != null)
                return;

            placingFrame = true;
            placeObj = placeObj.rootGrabbable;

            if (placeObj.placePoint != null && placeObj.placePoint != this)
                placeObj.placePoint.Remove(placeObj);

            placedObject = placeObj.rootGrabbable;
            placedObject.SetPlacePoint(this);

            if ((forceHandRelease || disableRigidbodyOnPlace) && placeObj.HeldCount() > 0)
            {
                placeObj.ForceHandsRelease();
                foreach (var grab in placeObj.grabbableChildren)
                    grab.ForceHandsRelease();
            }

            if (matchPosition)
                placeObj.rootTransform.position = placedOffset.position;
            if (matchRotation)
                placeObj.rootTransform.rotation = placedOffset.rotation;

            if (placeObj.body != null)
            {
                placeObj.body.linearVelocity = Vector3.zero;
                placeObj.body.angularVelocity = Vector3.zero;
                placedObjDetectionMode = placeObj.body.collisionDetectionMode;

                if (makePlacedKinematic && !disableRigidbodyOnPlace)
                {
                    placeObj.body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    placeObj.body.isKinematic = makePlacedKinematic;
                }

                if (placedJointLink != null)
                {
                    joint = placedJointLink.gameObject.AddComponent<FixedJoint>();
                    joint.connectedBody = placeObj.body;
                    joint.breakForce = jointBreakForce;
                    joint.breakTorque = jointBreakForce;

                    joint.connectedMassScale = 1;
                    joint.massScale = 1;
                    joint.enableCollision = false;
                    joint.enablePreprocessing = false;
                }
            }

            StopHighlight(placeObj);

            foreach (var grab in placeObj.grabbableChildren)
                grab.OnGrabEvent += OnPlaceObjectChildGrabbed;

            placeObj.OnPlacePointAddEvent?.Invoke(this, placeObj);
            foreach (var grabChild in placeObj.grabbableChildren)
                grabChild.OnPlacePointAddEvent?.Invoke(this, grabChild);

            OnPlaceEvent?.Invoke(this, placeObj);
            OnPlace?.Invoke(this, placeObj);
            lastPlacedTime = Time.time;

            if (destroyObjectOnPlace)
            {
                Destroy(placeObj.gameObject);
                return;
            }

            if (parentOnPlace)
            {
                placeObj.rootTransform.parent = transform;
            }

            if (disableRigidbodyOnPlace)
                placeObj.DeactivateRigidbody();

            if (disablePlacePointOnPlace)
                enabled = false;

            if (disableGrabOnPlace || disablePlacePointOnPlace)
                placeObj.isGrabbable = false;

            if (resizeOnPlace)
            {
                placeObj.OnBeforeGrabEvent += ResizeBeforeGrab;
                foreach (var grabbable in placeObj.grabbableChildren)
                    grabbable.OnBeforeGrabEvent += ResizeBeforeGrab;

                placeObj.OnGrabEvent += RecacluatePoseAfterGrab;
                foreach (var grabChild in placeObj.rootGrabbable.grabbableChildren)
                    grabChild.OnGrabEvent += RecacluatePoseAfterGrab;

                prefitScale = placeObj.rootTransform.localScale;
                wasInstantGrab = placeObj.instantGrab;
                placeObj.instantGrab = true;

                var scale = Mathf.Abs(transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y);
                scale = Mathf.Abs(scale < transform.lossyScale.z ? scale : transform.lossyScale.z);
                if (shapeType == PlacePointShape.Sphere)
                    FitAndCenterToBounds(placeObj.rootTransform.gameObject, placeRadius * scale + resizeOffset * scale);
                else if (shapeType == PlacePointShape.Box)
                    FitAndCenterToBounds(placeObj.rootTransform.gameObject, (placeSize + placeSize * resizeOffset) * scale);
            }

            if (grabbablePlacePoint)
            {
                placeObj.OnBeforeGrabEvent += RecalculateBeforeGrab;
                foreach (var grabbable in placeObj.grabbableChildren)
                    grabbable.OnBeforeGrabEvent += RecalculateBeforeGrab;
            }

            if (parentGrabbable != null)
            {
                foreach (var childPlacePoint in placeObj.childPlacePoints)
                {
                    parentGrabbable.PlacePointIgnore(childPlacePoint);
                    childPlacePoint.StopHighlight();
                    childPlacePoint.enabled = false;
                    if (childPlacePoint.placedObject != null)
                        childPlacePoint.placedObject.enabled = false;
                }
                if (disableRigidbodyOnPlace && parentOnPlace)
                    parentGrabbable.AddGrabbableColliders(placeObj);
            }

            // A questo punto aggiorniamo la griglia
            if (gridGenerator != null)
            {
                Note noteComponent = placeObj.GetComponent<Note>();
                if (noteComponent != null)
                {
                    NoteData.NoteDuration duration = noteComponent.noteData.duration;
                    int x = Mathf.FloorToInt((placedOffset.position.x - gridGenerator.transform.position.x) / gridGenerator.cellSize.value);
                    int y = Mathf.FloorToInt((placedOffset.position.y - gridGenerator.transform.position.y) / gridGenerator.cellSize.value);
                    int z = Mathf.FloorToInt((placedOffset.position.z - gridGenerator.transform.position.z) / gridGenerator.cellSize.value);

                    placedX = x;
                    placedY = y;
                    placedZ = z;

                    gridGenerator.PlaceNoteInGrid(x, y, z, placeObj.gameObject, duration);
                }
            }
        }

        public virtual void Remove(Grabbable placeObj)
        {
            placeObj = placeObj.rootGrabbable;

            if (placeObj == null || placeObj != placedObject || disablePlacePointOnPlace)
                return;

            foreach (var grab in placeObj.grabbableChildren)
                grab.OnGrabEvent -= OnPlaceObjectChildGrabbed;

            Highlight(placeObj);

            if (disableRigidbodyOnPlace)
                placeObj.ActivateRigidbody();

            if (placeObj.body != null)
            {
                if (makePlacedKinematic && !disableRigidbodyOnPlace)
                    placeObj.body.isKinematic = false;

                placeObj.body.collisionDetectionMode = placedObjDetectionMode;
            }

            if (parentGrabbable != null)
            {
                if (placeObj.childPlacePoints.Count > 0)
                {
                    foreach (var childPlacePoint in placeObj.childPlacePoints)
                    {
                        parentGrabbable.PlacePointAllow(childPlacePoint);
                        childPlacePoint.enabled = true;
                        if (childPlacePoint.placedObject != null)
                            childPlacePoint.placedObject.enabled = true;
                    }
                }

                parentGrabbable.RemoveGrabbableColliders(placeObj);
                parentGrabbable.IgnoreGrabbableCollisionUntilNone(placeObj);
                foreach (var hand in parentGrabbable.GetHeldBy())
                    placeObj.IgnoreHandCollisionUntilNone(hand);
                foreach (var hand in placeObj.GetHeldBy())
                    parentGrabbable.IgnoreHandCollisionUntilNone(hand);
            }

            if (resizeOnPlace)
            {
                placedObject.OnBeforeGrabEvent -= ResizeBeforeGrab;
                foreach (var grabbable in placedObject.grabbableChildren)
                    grabbable.OnBeforeGrabEvent -= ResizeBeforeGrab;

                if (placeObj.HeldCount() == 0)
                    placeObj.rootTransform.localScale = prefitScale;

                placeObj.instantGrab = wasInstantGrab;
            }

            if (grabbablePlacePoint)
            {
                placedObject.OnBeforeGrabEvent -= RecalculateBeforeGrab;
                foreach (var grabbable in placedObject.grabbableChildren)
                    grabbable.OnBeforeGrabEvent -= RecalculateBeforeGrab;
            }

            if ((!placeObj.parentOnGrab || placeObj.HeldCount() == 0) && parentOnPlace && gameObject.activeInHierarchy)
            {
                placeObj.rootTransform.parent = placeObj.originalParent;
            }

            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }

            placedObject.OnPlacePointRemoveEvent?.Invoke(this, highlightingObj);
            foreach (var grabChild in placedObject.grabbableChildren)
                grabChild.OnPlacePointRemoveEvent?.Invoke(this, grabChild);
            OnRemoveEvent?.Invoke(this, placeObj);
            OnRemove?.Invoke(this, placeObj);

            lastPlacedObject = placedObject;

            // Rimuoviamo dalla griglia
            if (gridGenerator != null && placedObject != null)
            {
                Note noteComponent = placedObject.GetComponent<Note>();
                if (noteComponent != null)
                {
                    NoteData.NoteDuration duration = noteComponent.noteData.duration;
                    gridGenerator.RemoveNoteFromGrid(placedX, placedY, placedZ, duration);
                }
            }

            placedObject = null;
        }

        [ContextMenu("Remove Placed")]
        public void Remove()
        {
            if (placedObject != null)
                Remove(placedObject);
        }

        public virtual void Highlight(Grabbable from)
        {
            from = from.rootGrabbable;
            if (highlightingObj == null)
            {
                highlightingObj = from;
                from.SetPlacePoint(this);

                highlightingObj.OnPlacePointHighlightEvent?.Invoke(this, highlightingObj);
                foreach (var grabChild in highlightingObj.grabbableChildren)
                    grabChild.OnPlacePointHighlightEvent?.Invoke(this, grabChild);

                OnHighlightEvent?.Invoke(this, from);
                OnHighlight?.Invoke(this, from);

                if (placedObject == null && (forcePlace || (!heldPlaceOnly && from.HeldCount() == 0)))
                    Place(from);
            }
        }

        public virtual void StopHighlight(Grabbable grab)
        {
            grab = grab.rootGrabbable;
            if (highlightingObj == grab)
            {
                StopHighlight();
            }
        }

        public virtual void StopHighlight()
        {
            if (highlightingObj != null)
            {
                highlightingObj.OnPlacePointUnhighlightEvent?.Invoke(this, highlightingObj);
                foreach (var grabChild in highlightingObj.grabbableChildren)
                    grabChild.OnPlacePointUnhighlightEvent?.Invoke(this, grabChild);

                OnStopHighlightEvent?.Invoke(this, highlightingObj);
                OnStopHighlight?.Invoke(this, highlightingObj);

                if (placedObject == null)
                    highlightingObj.SetPlacePoint(null);

                highlightingObj = null;
            }
        }

        int lastOverlapCount = 0;
        protected virtual IEnumerator CheckPlaceObjectLoop()
        {
            yield return new WaitForSeconds(0.2f);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, tickRate));

            while (gameObject.activeInHierarchy)
            {
                var scale = Mathf.Abs(transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y);
                scale = Mathf.Abs(scale < transform.lossyScale.z ? scale : transform.lossyScale.z);
                if (!disablePlacePointOnPlace && !disableRigidbodyOnPlace && placedObject != null &&
                    lastPlacePosition != placedObject.transform.position && !IsStillOverlapping(placedObject, scale) && !placingFrame)
                {
                    Remove(placedObject);
                }

                if (placedObject != null)
                    lastPlacePosition = placedObject.transform.position;

                CheckHighlight(scale);

                yield return new WaitForSeconds(tickRate);
                if (placedObject != null && placingFrame)
                {
                    if (matchPosition)
                        placedObject.rootTransform.position = placedOffset.position;
                    if (matchRotation)
                        placedObject.rootTransform.rotation = placedOffset.rotation;

                    lastPlacePosition = placedObject.transform.position;
                }

                placingFrame = false;
            }
        }

        protected virtual void CheckHighlight(float scale)
        {
            if (placedObject == null && highlightingObj == null)
            {
                var overlapCenterPos = placedOffset.position + transform.rotation * shapeOffset;
                int overlaps = 0;
                switch (shapeType)
                {
                    case PlacePointShape.Sphere:
                        overlaps = Physics.OverlapSphereNonAlloc(overlapCenterPos, placeRadius * scale, collidersNonAlloc, placeLayers);
                        break;
                    case PlacePointShape.Box:
                        overlaps = Physics.OverlapBoxNonAlloc(overlapCenterPos, placeSize / 2f * scale, collidersNonAlloc, transform.rotation, placeLayers);
                        break;
                }

                if (overlaps != lastOverlapCount)
                {
                    var updateOverlaps = true;
                    for (int i = 0; i < overlaps; i++)
                    {
                        if (AutoHandExtensions.HasGrabbable(collidersNonAlloc[i].gameObject, out var tempGrabbable))
                        {
                            tempGrabbable = tempGrabbable.rootGrabbable;
                            updateOverlaps = false;

                            if (CanPlace(tempGrabbable))
                            {
                                var existingPlacePoint = tempGrabbable.placePoint;
                                if (existingPlacePoint)
                                {
                                    var grabbablePos = tempGrabbable.transform.position;
                                    var concurrentCenterPos = existingPlacePoint.placedOffset.position + existingPlacePoint.transform.rotation * existingPlacePoint.shapeOffset;
                                    var concurrentDist = Vector3.Distance(concurrentCenterPos, grabbablePos);
                                    var currentDist = Vector3.Distance(overlapCenterPos, grabbablePos);
                                    if (currentDist >= concurrentDist)
                                        continue;

                                    existingPlacePoint.StopHighlight(tempGrabbable);
                                }

                                Highlight(tempGrabbable);
                                break;
                            }
                        }
                    }

                    if (updateOverlaps)
                    {
                        lastOverlapCount = overlaps;
                    }
                }
            }
            else if (highlightingObj != null)
            {
                if (!IsStillOverlapping(highlightingObj, scale))
                {
                    StopHighlight(highlightingObj);
                }
            }
        }

        protected bool IsStillOverlapping(Grabbable from, float scale = 1)
        {
            var overlapCenterPos = placedOffset.position + transform.rotation * shapeOffset;
            int overlaps = 0;
            switch (shapeType)
            {
                case PlacePointShape.Sphere:
                    overlaps = Physics.OverlapSphereNonAlloc(overlapCenterPos, placeRadius * scale, collidersNonAlloc, placeLayers);
                    break;
                case PlacePointShape.Box:
                    overlaps = Physics.OverlapBoxNonAlloc(overlapCenterPos, placeSize / 2f * scale, collidersNonAlloc, transform.rotation, placeLayers);
                    break;
            }

            for (int i = 0; i < overlaps; i++)
            {
                if (collidersNonAlloc[i].attachedRigidbody == from.body)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual void OnPlaceObjectChildGrabbed(Hand pHand, Grabbable pGrabbable)
        {
            Remove();
        }

        protected void ResizeBeforeGrab(Hand hand, Grabbable grab)
        {
            grab.rootTransform.localScale = prefitScale;
            Physics.SyncTransforms();
            if (grab.body != null)
            {
                grab.body.WakeUp();
                grab.body.detectCollisions = false;
                grab.body.detectCollisions = true;
            }

        }

        protected void RecalculateBeforeGrab(Hand hand, Grabbable grab)
        {
            hand.RecalculateBeforeGrab(grab);
        }

        protected void RecacluatePoseAfterGrab(Hand hand, Grabbable grab)
        {
            hand.RecaculateHeldAutoPose();
            grab.rootGrabbable.OnGrabEvent -= RecacluatePoseAfterGrab;
            foreach (var grabChild in grab.rootGrabbable.grabbableChildren)
            {
                grabChild.OnGrabEvent -= RecacluatePoseAfterGrab;
            }
        }

        protected void FitAndCenterToBounds(GameObject obj, float radius)
        {
            Bounds bounds = CalculateCombinedBounds(obj);
            var scaleOffset = ScaleToFitRadius(obj, bounds, radius);
            obj.transform.localScale *= scaleOffset;
            bounds.extents *= scaleOffset;
            bounds = CalculateCombinedBounds(obj);
            if (matchPosition)
                obj.transform.position = placedOffset.position + (obj.transform.position - bounds.center);
            if (matchRotation)
                obj.transform.rotation = placedOffset.rotation;
        }

        protected float ScaleToFitRadius(GameObject obj, Bounds bounds, float radius)
        {
            float maxExtent = bounds.extents.magnitude;
            float scale = radius / maxExtent;
            return scale;
        }
        protected void FitAndCenterToBounds(GameObject obj, Vector3 size)
        {
            Bounds bounds = CalculateCombinedBounds(obj);
            float scaleOffset = ScaleToFitSize(obj, bounds, size);
            obj.transform.localScale *= scaleOffset;
            bounds.extents *= scaleOffset;
            bounds = CalculateCombinedBounds(obj);
            if (matchPosition)
                obj.transform.position = placedOffset.position + (obj.transform.position - bounds.center);
            if (matchRotation)
                obj.transform.rotation = placedOffset.rotation;
        }

        protected float ScaleToFitSize(GameObject obj, Bounds bounds, Vector3 size)
        {
            Vector3 currentSize = bounds.size;
            float scaleX = size.x / currentSize.x;
            float scaleY = size.y / currentSize.y;
            float scaleZ = size.z / currentSize.z;
            float scale = Mathf.Min(scaleX, Mathf.Min(scaleY, scaleZ));
            return scale;
        }

        protected Bounds CalculateCombinedBounds(GameObject obj)
        {
            var meshRenderers = obj.GetComponentsInChildren<MeshRenderer>().OfType<Renderer>();
            var skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>().OfType<Renderer>();
            List<Renderer> renderers = new List<Renderer>();

            renderers.AddRange(meshRenderers);
            renderers.AddRange(skinnedMeshRenderers);
            Bounds combinedBounds = new Bounds(obj.transform.position, Vector3.zero);

            foreach (Renderer renderer in renderers)
                combinedBounds.Encapsulate(renderer.bounds);

            return combinedBounds;
        }

        protected virtual void OnJointBreak(float breakForce)
        {
            if (placedObject != null)
                Remove(placedObject);
        }

        void OnDrawGizmos()
        {
            if (placedOffset == null)
                placedOffset = transform;

            var scale = Mathf.Abs(transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y);
            scale = Mathf.Abs(scale < transform.lossyScale.z ? scale : transform.lossyScale.z);

            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (shapeType == PlacePointShape.Box)
            {
                Gizmos.DrawWireCube(shapeOffset, placeSize);

                if (resizeOffset != 0 && resizeOnPlace)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(shapeOffset, (placeSize + placeSize * resizeOffset));
                }
            }
            else if (shapeType == PlacePointShape.Sphere)
            {

                Gizmos.DrawWireSphere(shapeOffset, placeRadius);

                if (resizeOffset != 0 && resizeOnPlace)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(shapeOffset, placeRadius + resizeOffset);
                }
            }
        }

        void IGrabbableEvents.OnHighlight(Hand hand)
        {
            if (!grabbablePlacePoint)
                return;
            if (placedObject != null)
                placedObject.Highlight(hand);
        }

        public virtual void OnUnhighlight(Hand hand)
        {
            if (!grabbablePlacePoint)
                return;
            if (placedObject != null)
                placedObject.Unhighlight(hand);
        }

        public virtual void OnGrab(Hand hand)
        {
            if (!grabbablePlacePoint)
                return;
            hand.RecaculateHeldAutoPose();
        }

        public virtual void OnRelease(Hand hand)
        {
            if (!grabbablePlacePoint)
                return;
        }

        public virtual bool CanGrab(Hand hand)
        {
            if (!grabbablePlacePoint || placedObject == null)
                return false;

            return placedObject.CanGrab(hand);
        }

        public virtual Grabbable GetGrabbable()
        {
            if (!grabbablePlacePoint || placedObject == null || !enabled)
                return null;

            return placedObject;
        }
    }
}

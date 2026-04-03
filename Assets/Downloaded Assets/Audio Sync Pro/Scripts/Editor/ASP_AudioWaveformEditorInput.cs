/*******************************************************
Product - Audio Sync Pro
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2024
  (c) 2024 Martin Hansen. All rights reserved.
/*******************************************************/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TelePresent.AudioSyncPro
{
    public class ASP_AudioWaveformEditorInput : IDisposable
    {
        private const float BoxSelectThreshold = 5f;
        private const float MarkerProximityThreshold = 0.00001f;  // Adjusted for normalized time
        private const float MinWaveformHeight = 50f; // Minimum height for the waveform
        public const float maxZoom = 20f; // Maximum zoom level for the waveform

        private Vector2 lastMousePosition;
        private bool isDraggingMarker = false;
        private int draggingMarkerIndex = -1;
        public bool isBoxSelecting = false;
        private Vector2 boxStartPos;
        public Rect selectionBox;

        private AudioSourcePlus audioSourcePlus;
        public bool isResizingWaveform = false;

        public ASP_AudioWaveformEditorInput(AudioSourcePlus _audioSourcePlus)
        {
            audioSourcePlus = _audioSourcePlus;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void HandlePlayheadInput(ref bool isDraggingPlayhead, ref float playheadPosition, Rect playheadRect, Rect waveformRect, float viewStart, float viewEnd)
        {
            Event currentEvent = Event.current;

            // Check if the left mouse button (button 0) is pressed
            if (currentEvent.button != 0) // Ignore if it's not a left click
            {
                return;
            }

            // Check if it's a mouse down event to initiate click
            if (currentEvent.type == EventType.MouseDown)
            {
                // **Remove focus from any other control and set focus to the waveform**
                GUI.FocusControl(null);

                // **Check if the mouse is over any marker only on MouseDown**
                if (IsMouseOverAnyMarker(currentEvent.mousePosition, audioSourcePlus.markers, waveformRect, viewStart, viewEnd))
                {
                    return; // Do nothing if the mouse is over a marker
                }

                // Allow starting drag if mouse is over the playhead, regardless of waveformRect
                if (playheadRect.Contains(currentEvent.mousePosition))
                {
                    if (currentEvent.clickCount == 2) // Check for double-click
                    {
                        // Add a marker at the playhead position
                        CreateMarker(audioSourcePlus.markers, waveformRect, viewStart, viewEnd, audioSourcePlus.audioSource.clip);
                        return;
                    }

                    isDraggingPlayhead = true;
                    GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    currentEvent.Use();
                }
            }

            // Allow dragging playhead if it is already being dragged
            if (isDraggingPlayhead && currentEvent.type == EventType.MouseDrag)
            {
                float mousePositionX = Mathf.Clamp(currentEvent.mousePosition.x, waveformRect.x, waveformRect.xMax);
                float visiblePlayheadPosition = (mousePositionX - waveformRect.x) / waveformRect.width;
                playheadPosition = viewStart + visiblePlayheadPosition * (viewEnd - viewStart);
                audioSourcePlus.audioSource.time = playheadPosition * audioSourcePlus.audioSource.clip.length;
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseUp && isDraggingPlayhead)
            {
                isDraggingPlayhead = false;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }
        }


        public void HandleMouseInput(ref bool isDraggingView, ref float viewStart, ref float viewEnd, ref Rect waveformRect)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 2 && waveformRect.Contains(currentEvent.mousePosition))
            {
                isDraggingView = true;
                lastMousePosition = currentEvent.mousePosition;
                currentEvent.Use();
            }

            if (isDraggingView && currentEvent.type == EventType.MouseDrag && currentEvent.button == 2)
            {
                Vector2 mouseDelta = currentEvent.mousePosition - lastMousePosition;
                float viewRange = viewEnd - viewStart;
                float dragAmount = mouseDelta.x / waveformRect.width * viewRange;

                viewStart = Mathf.Clamp(viewStart - dragAmount, 0f, 1f - viewRange);
                viewEnd = Mathf.Clamp(viewEnd - dragAmount, viewRange, 1f);

                lastMousePosition = currentEvent.mousePosition;

                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 2 && isDraggingView)
            {
                isDraggingView = false;
                currentEvent.Use();
            }
        }

        public void HandleMouseScroll(ref float zoomLevel, ref float viewStart, ref float viewEnd, Rect waveformRect)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.ScrollWheel || !waveformRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            float scrollDelta = currentEvent.shift ? currentEvent.delta.x : currentEvent.delta.y;
            float viewRange = viewEnd - viewStart;

            if (currentEvent.shift)
            {
                float shiftAmount = scrollDelta * 0.01f * viewRange;

                viewStart = Mathf.Clamp(viewStart + shiftAmount, 0f, 1f - viewRange);
                viewEnd = Mathf.Clamp(viewEnd + shiftAmount, viewRange, 1f);
            }
            else
            {
                float mouseXRatio = (currentEvent.mousePosition.x - waveformRect.x) / waveformRect.width;
                float newZoomLevel = Mathf.Clamp(zoomLevel - scrollDelta * 0.1f, 0.5f, maxZoom);

                float oldRange = viewEnd - viewStart;
                float newRange = oldRange * (zoomLevel / newZoomLevel);

                float viewCenter = viewStart + oldRange * mouseXRatio;

                viewStart = Mathf.Clamp(viewCenter - newRange * mouseXRatio, 0f, 1f - newRange);
                viewEnd = Mathf.Clamp(viewCenter + newRange * (1 - mouseXRatio), newRange, 1f);

                zoomLevel = newZoomLevel;
            }

            currentEvent.Use();
        }


        private void HandleMarkerMouseDown(List<ASP_Marker> markers, Rect waveformRect, ref float viewStart, ref float viewEnd, AudioClip clip)
        {
            Event currentEvent = Event.current;

            if (currentEvent.clickCount == 2)
            {
                CreateMarker(markers, waveformRect, viewStart, viewEnd, clip);
                return;
            }

            for (int i = 0; i < markers.Count; i++)
            {
                if (IsMouseOverMarker(currentEvent.mousePosition, markers[i], waveformRect, viewStart, viewEnd, clip))
                {
                    // Start the drag operation but do not immediately deselect other markers
                    StartMarkerDrag(markers, i);
                    boxStartPos = currentEvent.mousePosition; // Track start position to differentiate drag
                    currentEvent.Use();
                    return;
                }
            }

            // If the click is not on any marker, deselect all markers
            foreach (var marker in markers)
            {
                marker.IsSelected = false;
            }

            boxStartPos = currentEvent.mousePosition;
            isBoxSelecting = false;
        }




        private void CreateMarker(List<ASP_Marker> markers, Rect waveformRect, float viewStart, float viewEnd, AudioClip clip)
        {
            Undo.RecordObject(audioSourcePlus, "Create Marker");  // Record undo state

            Event currentEvent = Event.current;

            // Deselect all existing markers
            foreach (var marker in markers)
            {
                marker.IsSelected = false;
            }

            float mouseXRatio = (currentEvent.mousePosition.x - waveformRect.x) / waveformRect.width;
            float newMarkerNormalizedTime = Mathf.Clamp(viewStart + mouseXRatio * (viewEnd - viewStart), 0f, 1f);

            foreach (var marker in markers)
            {
                if (Mathf.Abs(marker.normalizedTimelinePosition - newMarkerNormalizedTime) < MarkerProximityThreshold)
                {
                    return;  // Avoid adding a marker too close to an existing one
                }
            }

            ASP_Marker newMarker = new ASP_Marker(audioSourcePlus)
            {
                normalizedTimelinePosition = newMarkerNormalizedTime,
                Time = newMarkerNormalizedTime * clip.length,
                IsSelected = true  // Select the new marker
            };

            newMarker.MarkerBorn();
            markers.Add(newMarker);

            // **Sort the markers list by Time after adding the new marker**
            markers.Sort((a, b) => a.Time.CompareTo(b.Time));

            Undo.RecordObject(audioSourcePlus, "Create Marker");
            EditorUtility.SetDirty(audioSourcePlus);
            currentEvent.Use();
        }



        private void StartMarkerDrag(List<ASP_Marker> markers, int index)
        {
            isDraggingMarker = true;
            draggingMarkerIndex = index;

            // If the clicked marker is not selected, focus only on the clicked marker upon click and release
            if (!markers[index].IsSelected)
            {
                // Set up for potential deselection, but only execute if no drag occurs
                foreach (var marker in markers)
                {
                    marker.IsSelected = false;
                }
                markers[index].IsSelected = true;
            }

            Event.current.Use();
        }

        private void HandleMarkerMouseDrag(List<ASP_Marker> markers, Rect waveformRect, float viewStart, float viewEnd, AudioClip clip)
        {
            Event currentEvent = Event.current;

            // Check if a drag operation is starting
            if (!isDraggingMarker && Vector2.Distance(currentEvent.mousePosition, boxStartPos) > BoxSelectThreshold && waveformRect.Contains(boxStartPos))
            {
                isBoxSelecting = true;
            }

            // If dragging, allow multiple markers to be dragged
            if (isDraggingMarker)
            {
                // This code enables dragging multiple selected markers
                DragSelectedMarkers(markers, waveformRect, viewStart, viewEnd, clip);
                // Prevent deselecting other markers if dragging is detected
                isBoxSelecting = false;
            }

            // Update the selection box during drag
            if (isBoxSelecting && waveformRect.Contains(currentEvent.mousePosition))
            {
                selectionBox = NormalizeRect(new Rect(boxStartPos.x, boxStartPos.y, currentEvent.mousePosition.x - boxStartPos.x, currentEvent.mousePosition.y - boxStartPos.y));

                foreach (var marker in markers)
                {
                    float markerX = waveformRect.x + ((marker.normalizedTimelinePosition - viewStart) / (viewEnd - viewStart)) * waveformRect.width;
                    Rect markerRect = new Rect(markerX - 2, waveformRect.y, 4, waveformRect.height);

                    marker.IsSelected = selectionBox.Overlaps(markerRect);
                }

                currentEvent.Use();
            }
        }

        private void DragSelectedMarkers(List<ASP_Marker> markers, Rect waveformRect, float viewStart, float viewEnd, AudioClip clip)
        {
            Undo.RecordObject(audioSourcePlus, "Move Marker(s)");  // Record undo state

            Event currentEvent = Event.current;

            float mouseXRatio = (currentEvent.mousePosition.x - waveformRect.x) / waveformRect.width;
            float newNormalizedTime = Mathf.Clamp(viewStart + mouseXRatio * (viewEnd - viewStart), 0f, 1f);
            float timeDelta = newNormalizedTime - markers[draggingMarkerIndex].normalizedTimelinePosition;

            foreach (var marker in markers)
            {
                if (marker.IsSelected)
                {
                    marker.normalizedTimelinePosition = Mathf.Clamp(marker.normalizedTimelinePosition + timeDelta, 0f, 1f);
                    marker.Time = marker.normalizedTimelinePosition * clip.length;
                }
            }

            currentEvent.Use();
        }

        private void EndMarkerDrag(List<ASP_Marker> markers)
        {
            if (isDraggingMarker)
            {
                EditorUtility.SetDirty(audioSourcePlus); // Mark object as dirty on drag
            }

            // Distinguish between click-release and drag-end
            if (!isBoxSelecting && draggingMarkerIndex != -1 && Vector2.Distance(Event.current.mousePosition, boxStartPos) <= BoxSelectThreshold)
            {
                // Deselect all other markers if no drag occurred and focus on the clicked marker
                for (int i = 0; i < markers.Count; i++)
                {
                    markers[i].IsSelected = (i == draggingMarkerIndex);
                }
            }

            isBoxSelecting = false;
            isDraggingMarker = false;
            draggingMarkerIndex = -1;
            selectionBox = new Rect();
            Event.current.Use();
        }


        public void HandleMarkerInput(List<ASP_Marker> markers, Rect waveformRect, ref float viewStart, ref float viewEnd, AudioClip clip)
        {
            Event currentEvent = Event.current;

            // Handle Mouse Down event
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && waveformRect.Contains(currentEvent.mousePosition))
            {
                HandleMarkerMouseDown(markers, waveformRect, ref viewStart, ref viewEnd, clip);
            }

            // Handle Mouse Drag event
            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && waveformRect.Contains(currentEvent.mousePosition))
            {
                HandleMarkerMouseDrag(markers, waveformRect, viewStart, viewEnd, clip);
            }

            // Handle Mouse Up event
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
            {
                EndMarkerDrag(markers); // Ensure markers list is passed here
            }

            // Handle Right Click event
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1 && waveformRect.Contains(currentEvent.mousePosition))
            {
                HandleMarkerRightClick(markers, waveformRect, viewStart, viewEnd, clip);
            }

            // Handle Delete key to remove selected markers
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Delete)
            {
                Undo.RecordObject(audioSourcePlus, "Delete Marker(s)");  // Record undo state
                markers.RemoveAll(marker => marker.IsSelected);
                EditorUtility.SetDirty(audioSourcePlus);  // Mark object as dirty
                currentEvent.Use();
            }
            HandleMarkerKeyboardInput(markers, audioSourcePlus);

        }


        private void HandleMarkerRightClick(List<ASP_Marker> markers, Rect waveformRect, float viewStart, float viewEnd, AudioClip clip)
        {
            Event currentEvent = Event.current;

            for (int i = 0; i < markers.Count; i++)
            {
                if (IsMouseOverMarker(currentEvent.mousePosition, markers[i], waveformRect, viewStart, viewEnd, clip))
                {
                    Undo.RecordObject(audioSourcePlus, "Delete Marker");  // Record undo state
                    markers.RemoveAt(i);
                    EditorUtility.SetDirty(audioSourcePlus);  // Mark object as dirty on delete
                    currentEvent.Use();
                    return;
                }
            }
        }

        private Rect NormalizeRect(Rect rect)
        {
            if (rect.width < 0)
            {
                rect.x += rect.width;
                rect.width = -rect.width;
            }
            if (rect.height < 0)
            {
                rect.y += rect.height;
                rect.height = -rect.height;
            }
            return rect;
        }

        private bool IsMouseOverMarker(Vector2 mousePosition, ASP_Marker marker, Rect waveformRect, float viewStart, float viewEnd, AudioClip clip)
        {
            // Adjust hitbox height reduction
            float markerX = waveformRect.x + ((marker.normalizedTimelinePosition - viewStart) / (viewEnd - viewStart)) * waveformRect.width;
            float hitboxWidth = 20f; // Hitbox width remains the same
            float hitboxHeightReduction = 12f; // Adjust this value to make the hitbox shorter from the top

            // Adjust the top of the hitbox and reduce its height
            Rect markerHitbox = new Rect(
                markerX - hitboxWidth / 2,
                waveformRect.y + hitboxHeightReduction, // Move the top down by hitboxHeightReduction
                hitboxWidth,
                waveformRect.height - hitboxHeightReduction // Reduce the height from the top
            );

            return markerHitbox.Contains(mousePosition);
        }


        private bool IsMouseOverAnyMarker(Vector2 mousePosition, List<ASP_Marker> markers, Rect waveformRect, float viewStart, float viewEnd)
        {
            foreach (var marker in markers)
            {
                if (IsMouseOverMarker(mousePosition, marker, waveformRect, viewStart, viewEnd, audioSourcePlus.audioSource.clip))
                {
                    return true;
                }
            }
            return false;
        }

        public void OnGUI()
        {
            if (isBoxSelecting)
            {
                DrawSelectionBox();
            }
        }

        private void DrawSelectionBox()
        {
            if (selectionBox.width > 0 && selectionBox.height > 0)
            {
                Handles.BeginGUI();
                Handles.color = new Color(0.3f, 0.5f, 1f, 0.5f);
                Handles.DrawSolidRectangleWithOutline(selectionBox, new Color(0.3f, 0.5f, 1f, 0.25f), Color.blue);
                Handles.EndGUI();
            }
        }

        public void HandleKeyboardInput(AudioSourcePlus audioSource)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Space)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.PauseAudio();
                }
                else
                {
                    audioSource.PlayAudio();
                }
                currentEvent.Use();
            }
        }


        public void HandleMarkerKeyboardInput(List<ASP_Marker> markers, AudioSourcePlus audioSource)
        {
            Event currentEvent = Event.current;


            // Handle Copy (CTRL+C)
            if (currentEvent.type == EventType.KeyDown && currentEvent.control && currentEvent.keyCode == KeyCode.C)
            {
                CopySelectedMarker(markers);
                currentEvent.Use();  // Consume the event
            }

            // Handle Paste (CTRL+V)
            if (currentEvent.type == EventType.KeyDown && currentEvent.control && currentEvent.keyCode == KeyCode.V)
            {
                PasteMarker(markers, audioSource);
                currentEvent.Use();  // Consume the event
            }

            if (currentEvent.type == EventType.KeyDown && (currentEvent.keyCode == KeyCode.LeftArrow || currentEvent.keyCode == KeyCode.RightArrow))
            {
                if (markers.Count == 0) return; // If there are no markers, do nothing

                // Get the current normalized playhead position
                float currentPlayheadPosition = audioSource.playheadPosition;

                // Find the closest marker index based on the key pressed
                int closestMarkerIndex = -1;
                if (currentEvent.keyCode == KeyCode.RightArrow)
                {
                    // Find the next marker to the right
                    for (int i = 0; i < markers.Count; i++)
                    {
                        if (markers[i].normalizedTimelinePosition > currentPlayheadPosition)
                        {
                            closestMarkerIndex = i;
                            break;
                        }
                    }

                    // If no marker to the right, loop to the first marker on the left
                    if (closestMarkerIndex == -1)
                    {
                        closestMarkerIndex = 0; // Loop to the first marker
                    }
                }
                else if (currentEvent.keyCode == KeyCode.LeftArrow)
                {
                    // Find the previous marker to the left
                    for (int i = markers.Count - 1; i >= 0; i--)
                    {
                        if (markers[i].normalizedTimelinePosition < currentPlayheadPosition)
                        {
                            closestMarkerIndex = i;
                            break;
                        }
                    }

                    // If no marker to the left, loop to the last marker on the right
                    if (closestMarkerIndex == -1)
                    {
                        closestMarkerIndex = markers.Count - 1; // Loop to the last marker
                    }
                }

                // If a valid marker is found, move to it
                if (closestMarkerIndex != -1)
                {
                    // Deselect all markers
                    foreach (var marker in markers)
                    {
                        marker.IsSelected = false;
                    }

                    // Select the closest marker
                    markers[closestMarkerIndex].IsSelected = true;

                    // Set the playhead to the selected marker's time
                    audioSource.audioSource.time = markers[closestMarkerIndex].Time;
                    audioSource.playheadPosition = markers[closestMarkerIndex].normalizedTimelinePosition;

                    currentEvent.Use();  
                }
            }
        }


        private ASP_Marker copiedMarker;

        private void CopySelectedMarker(List<ASP_Marker> markers)
        {
            // Find the first selected marker and copy it
            foreach (var marker in markers)
            {
                if (marker.IsSelected)
                {
                    copiedMarker = marker.DeepCopy();  // Assuming DeepCopy is implemented in ASP_Marker

                    // Deep copy the DynamicPickers and their method parameters
                    if (copiedMarker.DynamicPickers != null)
                    {
                        copiedMarker.DynamicPickers = marker.DynamicPickers.Select(dp =>
                        {
                            var newPicker = new ASP_DynamicPicker
                            {
                                selectedGameObject = dp.selectedGameObject,
                                selectedComponent = dp.selectedComponent,
                                selectedMethodName = dp.selectedMethodName,
                                selectedComponentName = dp.selectedComponentName,
                                methodParameters = dp.methodParameters.Select(mp => mp.DeepCopy()).ToArray()
                            };
                            return newPicker;
                        }).ToList();
                    }

                    // Copy the ExecuteInEditMode state
                    copiedMarker.ExecuteInEditMode = marker.ExecuteInEditMode;

                    break;
                }
            }
        }

        private void PasteMarker(List<ASP_Marker> markers, AudioSourcePlus audioSource)
        {
            if (copiedMarker == null)
                return;

            Undo.RecordObject(audioSource, "Paste Marker");

            float playheadPosition = audioSource.playheadPosition;
            if (IsMarkerNearby(markers, playheadPosition))
                return;

            foreach (var marker in markers)
            {
                marker.IsSelected = false;
            }

            ASP_Marker newMarker = copiedMarker.DeepCopy();
            newMarker.Time = playheadPosition * audioSource.audioSource.clip.length;
            newMarker.normalizedTimelinePosition = playheadPosition;
            newMarker.IsSelected = true;

            // Deep copy the DynamicPickers and their method parameters
            if (newMarker.DynamicPickers != null)
            {
                newMarker.DynamicPickers = copiedMarker.DynamicPickers.Select(dp =>
                {
                    var newPicker = new ASP_DynamicPicker
                    {
                        selectedGameObject = dp.selectedGameObject,
                        selectedComponent = dp.selectedComponent,
                        selectedMethodName = dp.selectedMethodName,
                        selectedComponentName = dp.selectedComponentName,
                        methodParameters = dp.methodParameters.Select(mp => mp.DeepCopy()).ToArray()
                    };
                    return newPicker;
                }).ToList();
            }

            // Copy the ExecuteInEditMode state
            newMarker.ExecuteInEditMode = copiedMarker.ExecuteInEditMode;

            markers.Add(newMarker);
            markers.Sort((a, b) => a.Time.CompareTo(b.Time));

            EditorUtility.SetDirty(audioSource);
        }



        private bool IsMarkerNearby(List<ASP_Marker> markers, float position)
        {
            const float proximityThreshold = 0.01f;  
            foreach (var marker in markers)
            {
                if (Mathf.Abs(marker.normalizedTimelinePosition - position) < proximityThreshold)
                {
                    return true;  // Marker is too close
                }
            }
            return false;
        }
        public void HandleResizeHandle(ref bool isResizingWaveform, ref float waveformHeight, Rect waveformRect, float resizeHandleHeight)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown && IsMouseHoveringOverResizeHandle(waveformRect, resizeHandleHeight))
            {
                isResizingWaveform = true;
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                currentEvent.Use();
            }

            if (isResizingWaveform && currentEvent.type == EventType.MouseDrag)
            {
                waveformHeight = Mathf.Clamp(currentEvent.mousePosition.y - waveformRect.y, MinWaveformHeight, 800f); // Set limits for resizing
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseUp && isResizingWaveform)
            {
                isResizingWaveform = false;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }
        }

        public bool IsMouseHoveringOverResizeHandle(Rect waveformRect, float resizeHandleHeight)
        {
            Rect handleRect = new Rect(waveformRect.x, waveformRect.yMax - resizeHandleHeight, waveformRect.width, resizeHandleHeight);
            return handleRect.Contains(Event.current.mousePosition);
        }
    }
}
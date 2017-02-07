﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//By @JavierBullrich

namespace TileMapEditor
{
    [CustomEditor(typeof(TileMap))]
	public class TileMapEditor : Editor {

        public TileMap map;

        TileBrush brush;
        Vector3 mouseHitPos;

        bool mouseOnMap
        {
            get
            {
                return mouseHitPos.x > 0 && mouseHitPos.x < map.gridSize.x
                  && mouseHitPos.y < 0 && mouseHitPos.y > -map.gridSize.y;
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical();

            var oldSize = map.mapSize;
            map.mapSize=EditorGUILayout.Vector2Field("Map Size:", map.mapSize);
            if (map.mapSize != oldSize)
                UpdateCalculations();

            var oldTexture = map.texture2D;
            map.texture2D = (Texture2D)EditorGUILayout.ObjectField("Texture2D:", map.texture2D, typeof(Texture2D), false);

            if (oldTexture != map.texture2D) {
                UpdateCalculations();
                map.tileID = 1;
                CreateBrush();
            }

            if (map.texture2D == null)
            {
                EditorGUILayout.HelpBox("You have not selected a texture 2D yet.", MessageType.Warning);
            } else
            {
                EditorGUILayout.LabelField("Tile Size:", map.tileSize.x + "x" + map.tileSize.y);
                map.tilePadding = EditorGUILayout.Vector2Field("Tile Padding", map.tilePadding);
                EditorGUILayout.LabelField("Grid Size in Units:", map.gridSize.x + "x" + map.gridSize.y);
                EditorGUILayout.LabelField("Pixels To Units:", map.pixelsToUnits.ToString());
                UpdateBrush(map.currentTileBrush);

                if (GUILayout.Button("Clear Tiles"))
                    if (EditorUtility.DisplayDialog("Clear map's tiles?", "Are you sure?", "Clear", "Do not clear"))
                        ClearMap();
            }

            EditorGUILayout.EndVertical();
        }

        private void OnEnable()
        {
            map = target as TileMap;
            Tools.current = Tool.View;

            if (map.tiles == null) {
                var go = new GameObject("Tiles");
                go.transform.SetParent(map.transform);
                go.transform.position = Vector2.zero;

                map.tiles = go;
            }

            if (map.texture2D != null)
            {
                UpdateCalculations();
                NewBrush();
            }

        }

        private void OnDisable()
        {
            DestroyBrush();
        }

        private void OnSceneGUI()
        {
            if (brush != null)
            {
                UpdateHitPosition();
                MoveBrush();

                if (map.texture2D != null && mouseOnMap) {
                    Event current = Event.current;
                    if (current.shift)
                        Draw();
                    else if (current.alt)
                        RemoveTile();
                }
            }
        }


        private void UpdateCalculations()
        {
            var path = AssetDatabase.GetAssetPath(map.texture2D);
            map.spriteReferences = AssetDatabase.LoadAllAssetsAtPath(path);

            var sprite = (Sprite)map.spriteReferences[1];
            var width = sprite.textureRect.width;
            var height = sprite.textureRect.height;

            map.tileSize = new Vector2(width, height);
            // Calculate pixel to units
            map.pixelsToUnits = (int)(sprite.rect.width / sprite.bounds.size.x);
            map.gridSize = new Vector2((width / map.pixelsToUnits) * map.mapSize.x, (height / map.pixelsToUnits) * map.mapSize.y);
        }

        void CreateBrush()
        {
            var sprite = map.currentTileBrush;

            if (sprite != null)
            {
                GameObject go = new GameObject("Brush");
                go.transform.SetParent(map.transform);

                brush = go.AddComponent<TileBrush>();
                brush.renderer2D = go.AddComponent<SpriteRenderer>();
                brush.renderer2D.sortingOrder = 1000;
                var pixelsToUnits = map.pixelsToUnits;
                brush.brushSize = new Vector2(sprite.textureRect.width / pixelsToUnits, sprite.textureRect.height / pixelsToUnits);
                brush.UpdateBrush(sprite);
            }
        }

        void NewBrush()
        {
            if (brush == null)
            {
                CreateBrush();
            }
        }

        void DestroyBrush()
        {
            if (brush != null)
                DestroyImmediate(brush.gameObject);
        }

        public void UpdateBrush(Sprite sprite)
        {
            if (brush != null)
                brush.UpdateBrush(sprite);
        }

        private void UpdateHitPosition()
        {

            var p = new Plane(map.transform.TransformDirection(Vector3.forward), Vector3.zero);
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var hit = Vector3.zero;
            var dist = 0f;

            if (p.Raycast(ray, out dist))
                hit = ray.origin + ray.direction.normalized * dist;

            mouseHitPos = map.transform.InverseTransformPoint(hit);
        }

        void MoveBrush()
        {
            var tileSize = map.tileSize.x / map.pixelsToUnits;

            var x = Mathf.Floor(mouseHitPos.x / tileSize) * tileSize;
            var y = Mathf.Floor(mouseHitPos.y / tileSize) * tileSize;

            var row = x / tileSize;
            var column = Mathf.Abs(y / tileSize) - 1;

            if (!mouseOnMap)
                return;

            var id = (int)((column * map.mapSize.x) + row);

            brush.tileID = id;

            x += map.transform.position.x + tileSize / 2;
            y += map.transform.position.y + tileSize / 2;

            brush.transform.position = new Vector3(x, y, map.transform.position.z);
        }

        void Draw() {
            var id = brush.tileID.ToString();

            var posX = brush.transform.position.x;
            var posY = brush.transform.position.y;

            GameObject tile = GameObject.Find(map.name + "/Tiles/tile_" + id);

            if (tile == null) {
                tile = new GameObject("tile_" + id);
                tile.transform.SetParent(map.tiles.transform);
                tile.transform.position = new Vector3(posX, posY, 0);
                tile.AddComponent<SpriteRenderer>();
                
            }
            tile.GetComponent<SpriteRenderer>().sprite = brush.renderer2D.sprite;
        }

        void RemoveTile() {
            var id = brush.tileID.ToString();

            GameObject tile = GameObject.Find(map.name + "/Tiles/tile_" + id);

            if (tile != null)
                DestroyImmediate(tile);
        }

        void ClearMap() {
            for (int i = 0; i < map.tiles.transform.childCount; i++) {
                Transform t = map.tiles.transform.GetChild(i);
                DestroyImmediate(t.gameObject);
                i--;

            }
        }
    }
}
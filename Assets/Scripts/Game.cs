using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class Game : MonoBehaviour {
    //-------------------- Warriors --------------------//
    public List<Warrior> team;
    public GameObject lavaWormPrefab;
    private Warrior activeWarrior;
    private Warrior hoverWarrior;
    public List<Warrior> enemies;
    //-------------------- World --------------------//
    // TODO : Generative world objects
    public Tilemap terrain;
    public Tilemap tileEffects;
    public Tile hoverTile;
    public Tile rangeTile;
    private Vector3Int cursorTile;
    private Vector3Int prevTile;
    private List<Vector3Int> areaTiles;
    public Text debugText;
    //-------------------- Game Saves --------------------//
    public void SaveGame() {
        Save save = CreateSave();
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/gamesave.save");
        bf.Serialize(file, save);
        file.Close();
        Debug.Log("Game Saved");
    }
    public void LoadGame() {
        if (File.Exists(Application.persistentDataPath + "/gamesave.save")) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/gamesave.save", FileMode.Open);
            Save save = (Save) bf.Deserialize(file);
            file.Close();

            team = new List<Warrior>();
            for(int i = 0; i < save.cellsX.Count; i++) {
                Vector3Int cell = new Vector3Int(save.cellsX[i], save.cellsY[i], 0);
                float moveRegen = save.moveRegens[i];
                Warrior warrior = Instantiate(lavaWormPrefab, terrain.GetCellCenterWorld(cell), Quaternion.identity).GetComponent<Warrior>();
                warrior.SetCell(cell);
                warrior.SetCooldownMove(moveRegen);
                team.Add(warrior);
            }
            Debug.Log("Game Loaded seconds:" + save.saveTime);
        } else {
            NewGame();
        }
    }
    public void NewGame() {
        // TODO : Load both teams
        team = new List<Warrior>();
        Warrior warrior1 = Instantiate(lavaWormPrefab, terrain.GetCellCenterWorld(new Vector3Int(2, 0, 0)), Quaternion.identity).GetComponent<Warrior>();
        warrior1.SetCell(new Vector3Int(2, 0, 0));
        Warrior warrior2 = Instantiate(lavaWormPrefab, terrain.GetCellCenterWorld(new Vector3Int(-2, 1, 0)), Quaternion.identity).GetComponent<Warrior>();
        warrior2.SetCell(new Vector3Int(-2, 1, 0));
        team.Add(warrior1);
        team.Add(warrior2);
        Debug.Log("New Game");
    }
    
    private Save CreateSave() {
        Save save = new Save();
        save.saveTime = (float) (DateTime.UtcNow - 
               new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        foreach(Warrior warrior in team) {
            save.cellsX.Add(warrior.Cell().x);
            save.cellsY.Add(warrior.Cell().y);
            save.moveRegens.Add(warrior.CooldownMove());
        }
        return save;
    }
    //-------------------- Main Game Logic --------------------//
    void Start() {
        cursorTile = Vector3Int.zero;
        prevTile = cursorTile;
        tileEffects.ClearAllTiles();
        LoadGame();
    }
    private void OnApplicationPause(bool pauseStatus) {
        if(pauseStatus)
            SaveGame();
        else
            LoadGame();
    }
    void Update() {
        Vector3 cursorPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorTile = gameObject.GetComponent<Grid>().WorldToCell(cursorPoint);
        cursorTile.z = 0;
        // Hover and tiles
        if(prevTile != cursorTile) {
            hoverWarrior = null;
            foreach (Warrior warrior in team)
                if(warrior.Cell() == cursorTile)
                    hoverWarrior = warrior;
            if(hoverWarrior != null) {
                HighlightArea(hoverWarrior.Cell());
            } else {
                tileEffects.ClearAllTiles();
            }
            tileEffects.SetTile(cursorTile, hoverTile);
            prevTile = cursorTile;
        }
        // Click
        if(Input.GetMouseButtonDown(0)) {
            tileEffects.ClearAllTiles();
            if(hoverWarrior != null) {
                activeWarrior = hoverWarrior;
            } else {
                bool onMoveArea = false;
                // if(areaTiles != null)
                foreach(Vector3Int tile in areaTiles)
                    if(tile == cursorTile) onMoveArea = true;
                if(onMoveArea && activeWarrior.CanMove()) {
                    MoveWarrior(activeWarrior, cursorTile);
                    SaveGame();
                } else
                    activeWarrior = null;
            }
        }
        if(activeWarrior != null) {
            debugText.text = activeWarrior.CooldownMove() + "";
            HighlightArea(activeWarrior.Cell());
        }
    }

    private void MoveWarrior(Warrior activeWarrior, Vector3Int tile) {
        activeWarrior.Move(tile);
        activeWarrior.transform.position = terrain.GetCellCenterWorld(tile);
    }

    private void HighlightArea(Vector3Int positon) {
        // TODO : Recursion on range, include tile type modifiers
        areaTiles = new List<Vector3Int>();
        areaTiles.Add(new Vector3Int(positon.x-1, positon.y, 0));
        areaTiles.Add(new Vector3Int(positon.x, positon.y, 0));
        areaTiles.Add(new Vector3Int(positon.x+1, positon.y, 0));
        areaTiles.Add(new Vector3Int(positon.x, positon.y-1, 0));
        areaTiles.Add(new Vector3Int(positon.x, positon.y+1, 0));
        if(positon.y%2 == 0) {
            areaTiles.Add(new Vector3Int(positon.x-1, positon.y-1, 0));
            areaTiles.Add(new Vector3Int(positon.x-1, positon.y+1, 0));
        } else {
            areaTiles.Add(new Vector3Int(positon.x+1, positon.y-1, 0));
            areaTiles.Add(new Vector3Int(positon.x+1, positon.y+1, 0));
        }
        foreach(Vector3Int tile in areaTiles)
            tileEffects.SetTile(tile, rangeTile);
    }
}
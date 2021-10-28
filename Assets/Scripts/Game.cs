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
            
            ClearTeam();
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
        ClearTeam();
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
    // TODO : Clear Area tiles in all appropriate places
    // TODO : Find where duplicates are coming from and correct ClearTeam
    void ClearTeam() {
        foreach(Warrior warrior in team) {
            Destroy(warrior.GetComponent<GameObject>());
        }
        team = new List<Warrior>();
    }
    void Start() {
        NewGame();
        SaveGame();
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
                HighlightArea(hoverWarrior.Cell(), hoverWarrior.Range());
            } else {
                tileEffects.ClearAllTiles();
            }
            tileEffects.SetTile(cursorTile, hoverTile);
            prevTile = cursorTile;
            debugText.text = "" + cursorTile;
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
            HighlightArea(activeWarrior.Cell(), activeWarrior.Range());
        }
    }

    private void MoveWarrior(Warrior activeWarrior, Vector3Int tile) {
        activeWarrior.Move(tile);
        activeWarrior.transform.position = terrain.GetCellCenterWorld(tile);
    }
    /*¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯*
    |             Cell grid translates right 90º                                |
    |             Every mod(Y,2)=0 follows up > down > up                       |
    |             Every mod(Y,2)=1 follows down > up > down                     |
    |                                                                           |
    |                              so...                                        |
    |                 <     -      Y      +     >           cell = (0, 1):      |
    |    /¯¯¯\    ∧           mod(Y,2)=0 :                                      |
    |     X+1               /¯0¯\     /¯0¯\                      /¯1¯\          |
    |    \___/    +         \_0_//¯0¯\\_0_/                 /¯1¯\\_1_//¯1¯\     |
    |    /¯¯¯\                   \_0_/                      \_0_//¯0¯\\_2_/     |
    |      X      X                                         /¯0¯\\_1_//¯0¯\     |
    |    \___/                mod(Y,2)=1 :                  \_0_//-1¯\\_2_/     |
    |    /¯¯¯\    -              /¯0¯\                           \_1_/          |
    |     X-1               /¯0¯\\_0_//¯0¯\                                     |
    |    \___/    ∨         \_0_/     \_0_/                                     |
    |                                                       cell = (0, 2):      |
    |   Add each surrounding cell to List                                       |
    |   Break recursion when range is 1                          /¯1¯\          |
    |   Always check if List.Contains(cell)                 /¯0¯\\_2_//¯0¯\     |
    |                                                       \_1_//¯0¯\\_3_/     |
    |   To extend range, use TraceCell(0, true)             /-1¯\\_2_//-1¯\     |
    |                                                       \_1_//-1¯\\_3_/     |
    |                                                            \_2_/          |
    *__________________________________________________________________________*/
    private List<Vector3Int> myTiles = new List<Vector3Int>();
    private void AddToArea(Vector3Int cell, int distance) {
        int xOffset = (cell.y%2 == 0)? -1: 0;
        if(distance == 1) {
            Vector3Int plug = new Vector3Int(cell.x-1, cell.y, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            plug = new Vector3Int(cell.x, cell.y, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            plug = new Vector3Int(cell.x+1, cell.y, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            plug = new Vector3Int(cell.x+xOffset, cell.y-1, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            plug = new Vector3Int(cell.x+xOffset+1, cell.y-1, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            plug = new Vector3Int(cell.x+xOffset, cell.y+1, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            plug = new Vector3Int(cell.x+xOffset+1, cell.y+1, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
        } else {
            Vector3Int plug = new Vector3Int(cell.x-1, cell.y, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            AddToArea(plug, distance-1);
            plug = new Vector3Int(cell.x, cell.y, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            AddToArea(plug, distance-1);
            plug = new Vector3Int(cell.x+1, cell.y, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            AddToArea(plug, distance-1);
            plug = new Vector3Int(cell.x+xOffset, cell.y-1, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            AddToArea(plug, distance-1);
            plug = new Vector3Int(cell.x+xOffset+1, cell.y-1, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            AddToArea(plug, distance-1);
            plug = new Vector3Int(cell.x+xOffset, cell.y+1, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            AddToArea(plug, distance-1);
            plug = new Vector3Int(cell.x+xOffset+1, cell.y+1, 0);
            if(!myTiles.Contains(plug)) { myTiles.Add(plug); }
            AddToArea(plug, distance-1);
        }
    }

    private void HighlightArea(Vector3Int positon, int range) {
        // TODO : Recursion on range, include tile type modifiers
        myTiles = new List<Vector3Int>();
        areaTiles = new List<Vector3Int>();
        AddToArea(positon, range);
        areaTiles = myTiles;
        
        foreach(Vector3Int tile in areaTiles)
            tileEffects.SetTile(tile, rangeTile);
    }
}
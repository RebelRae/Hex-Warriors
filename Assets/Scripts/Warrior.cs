using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Warrior : MonoBehaviour {
    // TODO : Replace default values
    //-------------------- Environment --------------------//
    private int tileType = 0;
    private Vector3Int cell;
    //-------------------- Movement --------------------//
    private float moveMax = 10.0f;
    private float moveRegen = 0.1f;
    private float moveRemaining = 0.0f;
    private float moveCost = 4.3f;
    //-------------------- Attacks --------------------//
    private float baseAttack = 2.6f;
    private float attackMax = 3.2f;
    private float attackRegen = 0.05f;
    private float attackRemaining = 0.0f;
    private float attackCost = 4.3f;
    //-------------------- Stats --------------------//
    private float baseHitPoints = 12.5f;
    private List<float> weaknesses = new List<float>();
    private enum Element {
        Sand,
        Water,
        Lava,
        Snow,
        Grass,
        Psychic
    }
    private List<Element> elements = new List<Element>();

    void Start() { StartCoroutine(RegenerateStats(0.5f)); }
    //-------------------- Getters --------------------//
    public Vector3Int Cell() { return cell; }
    public bool CanMove() { return (moveRemaining > moveCost); }
    public bool CanAttack() { return (attackRemaining > attackCost); }
    public float CooldownMove() { return moveRemaining; }
    public float CooldownAttack() { return attackRemaining; }
    //-------------------- Setters --------------------//
    public void SetCooldownMove(float r) { moveRemaining = r; }
    public void SetCell(Vector3Int c) { cell = c; }
    public void Move(Vector3Int c) {
        SetCell(c);
        if(moveRemaining > moveCost) moveRemaining -= moveCost;
    }
    public void Attack(int attackType) { attackRemaining -= attackCost; }

    //-------------------- Stat Operations --------------------//
    private IEnumerator RegenerateStats(float secs) {
        while(true){
            yield return new WaitForSeconds(secs);
            if(moveRemaining < moveMax) moveRemaining += moveRegen;
            if(moveRemaining > moveMax) moveRemaining = moveMax;
            if(attackRemaining < attackMax) attackRemaining += attackRegen;
            if(attackRemaining > attackMax) attackRemaining = attackMax;
        }
    }
}

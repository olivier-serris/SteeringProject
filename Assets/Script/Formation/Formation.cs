﻿
using System.Collections.Generic;
using UnityEngine;
using System;
/*
 * Système  : 
 * Chaques unité cherche à atteindre une formation. 
 * Une formation peut être controllée et déplacée. 
 * 	1 Génération 
--> Une formation est un ensemble de positions liée entre elles. 
	--> Ces formation peuvent être générée en fonction d'une position et du nombre d'entités la composant.
	--> Ces peuvent être orienter et se déplacer.
	2 link agent -- emplacement :
Il existe une formation pour beaucoup d'entités. 
	--> Il faut lier les entités et les emplacement d'une formation.
	--> Une entité doit trouver l'emplacement à suivre. 
	--> On doit savoir si l'emplacement est pris ou non.
	-->
*/
//[RequireComponent(typeof(Steering))]
abstract public class Formation : MonoBehaviour {

    List<Steering> steeringUnits;
    public class Slot
    {
        public Vector3 position;
        public bool occupied;
        public Slot(Vector3 _pos) { position = _pos; occupied = false; }
    }
    protected List<Slot> slots;
    public Formation()
    {

    }

    public void Awake()
    {
        slots = new List<Slot>();
        steeringUnits = new List<Steering>();
    }
    virtual public Slot GetSlot(Vector3 position)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].occupied == false)
            {
                slots[i].occupied = true;
                return slots[i];
            }
        }
        Debug.Log("No position Found");
        return null;
    }
    public virtual void UpdateFormation(Vector3 origin, Quaternion rotation, int nbSlots)
    {
            // Check if slot need update
        if (transform.position == origin && rotation == transform.rotation && nbSlots == slots.Count)
            return;
        if (nbSlots <= 0)
            return;
            // remove unnecessary slots
        for (int i = slots.Count-1; i >= nbSlots; i--)
            slots.RemoveAt(i);
            // updateSlots
        for (int i = 0; i < nbSlots; i++)
        {
            if (i < slots.Count)
                slots[i].position = GetSlotPos(origin, rotation, nbSlots, i);
            else
                slots.Add(new Slot(GetSlotPos(origin, rotation, nbSlots, i)));
        }
    }

    abstract protected Vector3 GetSlotPos(Vector3 origin, Quaternion orientation,int nbSlots,int i);

    //public void FixedUpdate()
    //{
    //    UpdateFormation(transform.position, Quaternion.identity, steeringUnits.Count);
    //}

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (slots != null)
            for (int i = 0; i < slots.Count; i++)
                Gizmos.DrawSphere(slots[i].position, 0.25f);
    }
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceSteering : Steering {

     // Detect front object to walk on
    float maxFaceRayDistance;
    float faceSphereCastRadius;
        //Detect down object to walk on
    float maxDownRayDistance; 
    float downSphereCastRadius;

    float maxRotationOnTerrain; // rotate speed on ground
    float maxRotationDirection; // rotate speed toward direction
    float maxCatRotation;   // rotate when not touching ground to land on feet.

    float positionOnTerrainSpeed; // speed at wich the entity replace itself according to slope.

    Quaternion newRotation;
    LayerMask rayMask;
    public override void Awake()
    {
        base.Awake();
        string[] layerNametab = { "Default", "Ground", "Obstacle" };
        rayMask.value = LayerMask.GetMask(layerNametab);
        faceSphereCastRadius = 0.2f;
        downSphereCastRadius = 0.3f;
        maxRotationOnTerrain = 400;
        maxRotationDirection = 200;
        maxCatRotation = 500;

        positionOnTerrainSpeed = 3;
        maxFaceRayDistance = 2;
        maxDownRayDistance = agentCollExtent.y * 0.5f;

        newRotation = transform.rotation;
    }
    public override void Update()
    {
        //CheckGround();
        for (int i = 0; i < steeringBehaviorStack.Count; i++)
            steeringBehaviorStack[i].OnUpdate();
    }
    public override void FixedUpdate()
    {
        
        RaycastHit frontHit = new RaycastHit();
        RaycastHit backHit = new RaycastHit();
        bool hasFrontHit = false;
        bool hasBackHit = false;
        Vector3 averageNormal = Vector3.Lerp(OnPlanNormal, Vector3.up,0.1f);

            // Compute object hitted
        hasFrontHit = ComputeFrontHit(out frontHit);
        hasBackHit = ComputeBackHit(out backHit);

        IsOnGround = true;

        if (hasFrontHit && hasBackHit)
            averageNormal = PositionOnTerrain(frontHit, backHit);
        else if (hasFrontHit)
            averageNormal = PositionOnTerrain(frontHit, false);
        else if (hasBackHit)
            averageNormal = PositionOnTerrain(backHit, true);
        else
        {
            //if (!DebugCenterRay())
            {
                Debug.Log("Fall");
                RotateToNormalGround();
                Rb.MoveRotation(newRotation);
                IsOnGround = false;
                OnPlanNormal = averageNormal;
            }
            return;
        }
        OnPlanNormal = averageNormal;

            // compute all steering force and apply to velocity
        base.FixedUpdate();

        RotateToVelocity(averageNormal);
        Rb.MoveRotation(newRotation);
    }

    ////////// FIND Object around Creature with RayCast : 
    private bool ComputeFrontHit(out RaycastHit frontHit)
    {
        Vector3 transformUp = transform.up;
        Vector3 offsetZ = transform.forward * (AgentCollExtent.z+1);
        bool hasHit = false;
        /* SphereCast face au monstre
        On verifie que le monstre ne doit pas monter face à une obstruction. */
        Ray faceRay = new Ray(transform.position + transform.up* faceSphereCastRadius*1.1f, transform.forward);
        Debug.DrawLine(faceRay.origin, faceRay.origin+ faceRay.direction*(maxFaceRayDistance+ faceSphereCastRadius),Color.red);
        if (!(hasHit = Physics.SphereCast(faceRay, faceSphereCastRadius, out frontHit,
            maxFaceRayDistance - faceSphereCastRadius, rayMask.value,QueryTriggerInteraction.Ignore)))
        {
            /* BoxCast front down.  
            On essaie de récupérer ce sur quoi le monster marche en face de lui.*/
            Ray frontDownRay = new Ray(faceRay.origin + offsetZ, -transformUp);
            if (!(hasHit = Physics.SphereCast(frontDownRay, downSphereCastRadius, out frontHit,
                maxDownRayDistance , rayMask.value, QueryTriggerInteraction.Ignore)))
            {
                /* Raycast front down.  
                On essaie de récupérer ce sur quoi le monster marche en face de lui.*/
                Vector3 frontDownRayPosition = faceRay.origin - (transform.up * AgentCollExtent.y) + (transform.forward * AgentCollExtent.z );
                Ray frontDowBack = new Ray(frontDownRayPosition, -transform.forward);// check Raycast front down au monstre
                hasHit = Physics.Raycast(frontDowBack, out frontHit, maxDownRayDistance, rayMask.value,QueryTriggerInteraction.Ignore);
                frontHit.point = frontDownRay.origin + frontDownRay.direction * maxDownRayDistance;
            }
        }
        return hasHit;
    }
    private bool ComputeBackHit(out RaycastHit backHit)
    {
        bool hasHit = false;
        Vector3 transformUp = transform.up;
        Vector3 offsetZ = transform.forward * maxFaceRayDistance;

        Ray backRay = new Ray(transform.position + transform.up * faceSphereCastRadius * 1.1f, -offsetZ);
        if (!(hasHit = Physics.SphereCast(backRay, faceSphereCastRadius, out backHit,
            maxFaceRayDistance - faceSphereCastRadius, rayMask.value, QueryTriggerInteraction.Ignore)))
        {
            Ray backDownRay = new Ray(backRay.origin - offsetZ, -transformUp);
            hasHit = Physics.SphereCast(backDownRay, downSphereCastRadius, out backHit,
                maxDownRayDistance, rayMask.value, QueryTriggerInteraction.Ignore);
        }

        return hasHit;
    }
    ////////////// Reposition Creature according to surface walked on :: 

    private Vector3 PositionOnTerrain(RaycastHit frontHit, RaycastHit backHit)
    {
        Vector3 averageNormal = (frontHit.normal * 0.65f + backHit.normal * 0.35f);
        Vector3 averageHitPosition = (frontHit.point + backHit.point) / 2.0f;
        float step = Time.fixedDeltaTime * positionOnTerrainSpeed;
        float distance = (averageHitPosition - transform.position).magnitude;
        RotateOnPlan(averageNormal);
        Vector3 finalPosition = Vector3.MoveTowards(transform.position, averageHitPosition , step);

        Rb.MovePosition(finalPosition);
        return averageNormal;
    }
    private Vector3 PositionOnTerrain(RaycastHit hit, bool isBack)
    {
        Vector3 averageNormal;
        if (isBack)
            averageNormal = (hit.normal + transform.up) / 2.0f;
        else
            averageNormal = hit.normal;

        float step = Time.fixedDeltaTime * positionOnTerrainSpeed;
        RotateOnPlan(averageNormal);
        Vector3 finalPosition = Vector3.MoveTowards(transform.position, hit.point, step);

        Rb.MovePosition(finalPosition);
        return averageNormal;
    }
    ////////////// Rotate Creature according to surface walked on :: 
    void RotateOnPlan(Vector3 averageNormal) // rotate pour être posistionné correctement sur surface.
    {
        float angleStep = Time.fixedDeltaTime * maxRotationOnTerrain * (Rb.velocity.magnitude / maxSpeed);
        Vector3 planFoward = GetPlanDirection(transform.forward, averageNormal);
        Quaternion targetNormaRotation = Quaternion.LookRotation(planFoward, averageNormal);
        Quaternion finalRotation = Quaternion.RotateTowards(newRotation, targetNormaRotation, angleStep);
        newRotation = finalRotation;
    }
    private void RotateToVelocity(Vector3 averageNormal) // rotate vers direction ou on va
    {
        Vector3 planFoward = GetPlanDirection(Rb.velocity.normalized, averageNormal);
        if (planFoward != Vector3.zero)
        {
            Quaternion targetNormaRotation = Quaternion.LookRotation(planFoward, averageNormal);
            Quaternion finalRotation = Quaternion.RotateTowards(newRotation, targetNormaRotation, maxRotationDirection * Time.fixedDeltaTime);
            newRotation = finalRotation;
        }
    }
    private void RotateToNormalGround() // rotate pour retomber sur ses pieds
    {
        Vector3 planFoward = GetPlanDirection(transform.forward, Vector3.up);
        Quaternion targetNormaRotation = Quaternion.LookRotation(planFoward, Vector3.up);
        Quaternion finalRotation = Quaternion.RotateTowards(newRotation, targetNormaRotation, Time.fixedDeltaTime * maxCatRotation);
        newRotation = finalRotation;
    }

    public override Vector3 GetDvOnPlan(Vector3 target)
    {
        Vector3 dV = (target - transform.position);
        dV = Vector3.ClampMagnitude(dV, maxSpeed);
        dV = dV.normalized;
        if (Vector3.Dot(dV, OnPlanNormal) < 0)
        {
            Vector3 right = Vector3.Cross(dV, OnPlanNormal);
            Vector3 planDv = Vector3.Cross(OnPlanNormal, right);
            planDv.y = Mathf.Abs(planDv.y);
            return planDv.normalized * maxSpeed;
        }
        else
        {
            Vector3 right = Vector3.Cross(dV, OnPlanNormal);
            Vector3 planDv = Vector3.Cross(OnPlanNormal, right);
            planDv.y = -Mathf.Abs(planDv.y);
            return planDv.normalized * maxSpeed;
        }
    }
        // TODO : CHANGE THIS (NOT NOT NOT GOOD)
    bool isColliding;
    public bool DebugCenterRay()
    {
        if (isColliding)
        {
            transform.position += transform.forward * Time.fixedDeltaTime * maxSpeed;
            return true;
        }
        return false;
    }
    public void OnCollisionStay(Collision coll)
    {
        isColliding = true;
    }
    public void OnCollisionExit(Collision coll)
    {
        isColliding = false;
    }


}
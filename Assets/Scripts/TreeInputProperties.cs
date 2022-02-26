using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TreeInputProperties 
{
    public Branch root; 
    public float seed = 10f;
    // Random seed?
    public float rseed = 10f;
    public float trunkLength = 2.5f;
    public float initialBranchLength = 0.85f;
    public float branchFactor = 2f;
    public int levels = 3;
    public int treeSteps = 2;

    //int ??
    public float clumpMax = 0.8f;
    public float clumpMin = 0.5f;
    public float twistRate = 13f;

    public float growAmount = 0f;
    public float dropAmount = 0f;
    public float sweepAmount = 0f;
    public float lengthFalloffPower = 1f;
    public float lengthFalloffFactor = 0.85f;
    public float trunkKink = 0f;
    public float climbRate = 1.5f;
    public float taperRate = 0.95f;
    public float maxRadius = 0.25f;
    public int segments = 6;
    public float radiusFalloffRate = 0.6f;
    public float twigScale = 2f;
    public float vMultiplier = 0.2f;

    // Upgrades Pedro
    public float foliageAmountMult = 1f;
    public float leafLengthWidthRatio = 1f;
    public float leafCrossSectionRatio = 0.5f;
    public float leafDownwardTip = 0f;
    public int leafSegments = 1;
    public float leafParentBranchOffset = 0f;
    public int leavesPerBranchLine = 1;
    public int leavesPerBranchRing = 0;
    public float Random(float? val) {
        float nonNullSeed = val ?? this.rseed++;
        return Mathf.Abs(Mathf.Cos(nonNullSeed + nonNullSeed*nonNullSeed));
    }

}

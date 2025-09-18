// Copyright 2023-2024 Niantic.

using UnityEngine;
using System;

// Approximate geographic utility functions - only suitable for small offsets
public class Geographic
{
    public static double DEGREES_TO_METRES = 111139.0;
    public static double METRES_TO_DEGREES = 1.0 / DEGREES_TO_METRES;
    public static double LON_DEGREES_TO_TICKS = DEGREES_TO_METRES;
    public static double LON_TICKS_TO_DEGREES = 1.0 / LON_DEGREES_TO_TICKS;

    public struct LatLonRange
    {
        public double minLatitude;
        public double minLongitude;
        public double maxLatitude;
        public double maxLongitude;
    };

    public static LatLonRange expandRange(LatLonRange initialRangeDegrees, float metresToExpand)
    {
        LatLonRange expandedRange = new();
        double latitudeExpansionDegrees = metresToExpand*METRES_TO_DEGREES;
        double minCosLatitude = Math.Min(Math.Cos(initialRangeDegrees.minLatitude * Math.PI / 180.0), Math.Cos(initialRangeDegrees.maxLatitude * Math.PI / 180.0));
        double longitudeExpansionDegrees = metresToExpand*METRES_TO_DEGREES/minCosLatitude;
        expandedRange.minLatitude = initialRangeDegrees.minLatitude - latitudeExpansionDegrees;
        expandedRange.maxLatitude = initialRangeDegrees.maxLatitude + latitudeExpansionDegrees;
        expandedRange.minLongitude = initialRangeDegrees.minLongitude - longitudeExpansionDegrees;
        expandedRange.maxLongitude = initialRangeDegrees.maxLongitude + longitudeExpansionDegrees;
        return expandedRange;
    }

    public static double LatLonDistance(double latitudeDegreesA, double longitudeDegreesA, double latitudeDegreesB, double longitudeDegreesB)
    {
        return EastNorthOffset( latitudeDegreesA,  longitudeDegreesA,  latitudeDegreesB,  longitudeDegreesB).magnitude;
    }

    public static Vector2 EastNorthOffset(double latitudeDegreesA, double longitudeDegreesA, double latitudeDegreesB, double longitudeDegreesB)
    {
        float lonDifferenceMetres = (float)(Math.Cos((latitudeDegreesA+latitudeDegreesB)*0.5* Math.PI / 180.0) * (longitudeDegreesA - longitudeDegreesB) * DEGREES_TO_METRES);
        float latDifferenceMetres = (float)((latitudeDegreesA - latitudeDegreesB) * DEGREES_TO_METRES);
        return new Vector2(lonDifferenceMetres,latDifferenceMetres);
    }
}
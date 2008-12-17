
//	Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using Microsoft.NodeXL.Core;

namespace Microsoft.NodeXL.Algorithms
{
//*****************************************************************************
//  Interface: IDistanceCalculator
//
/// <summary>
///	Supports the calculation of distances between vertices.
/// </summary>
///
/// <remarks>
///	If a class that implements this interface uses an algorithm that does not
/// support a particular method, the unsupported method should throw a <see
/// cref="NotSupportedException" />.
/// </remarks>
//*****************************************************************************

public interface IDistanceCalculator
{
    //*************************************************************************
    //  Method: CalculateMinimumDistance()
    //
    /// <overloads>
    /// Calculates the minimum distance between vertices.
    /// </overloads>
	///
    /// <summary>
    /// Calculates the minimum distance between two specified vertices.
    /// </summary>
    ///
    /// <param name="vertexA">
	///	First vertex.  Must be one of the vertices in <paramref
	///	name="vertices" />.
    /// </param>
    ///
    /// <param name="vertexB">
	///	Second vertex.  Must be one of the vertices in <paramref
	///	name="vertices" />.
    /// </param>
    ///
    /// <param name="vertices">
	///	Collection of vertices.  <paramref name="vertexA" /> and <paramref
	///	name="vertexB" /> must be in the collection.
    /// </param>
    ///
    /// <param name="minimumDistance">
	///	Where the minimum distance gets stored if true is returned.
    /// </param>
	///
    /// <returns>
	///	true if there is at least one path between <paramref name="vertexA" />
	///	and <paramref name="vertexB" />, false if not.
    /// </returns>
    ///
    /// <remarks>
	///	If there is at least one path between <paramref name="vertexA" /> and
	///	<paramref name="vertexB" />, this method calculates the shortest
	///	distance between the two vertices, stores the distance at <paramref
	///	name="minimumDistance" />, and returns true.  Otherwise, false is
	///	returned.
    /// </remarks>
    //*************************************************************************

    Boolean
    CalculateMinimumDistance
    (
        IVertex vertexA,
        IVertex vertexB,
		IVertexCollection vertices,
		out Int32 minimumDistance
    );

    //*************************************************************************
    //  Method: CalculateMinimumDistance()
    //
    /// <summary>
    /// Calculates the minimum distance from a specified vertex to all other
	///	vertices.
    /// </summary>
    ///
    /// <param name="vertex">
	///	Vertex to calculate the minimum distance from.  Must be one of the
	/// vertices in <paramref name="vertices" />.
    /// </param>
    ///
    /// <param name="vertices">
	///	Collection of vertices.  <paramref name="vertex" /> must be in the
	/// collection.
    /// </param>
    ///
    /// <param name="minimumDistance">
	///	Where the minimum distance gets stored if true is returned.
    /// </param>
	///
    /// <returns>
	///	true if all the vertices in <paramref name="vertices" /> can be reached
	/// from <paramref name="vertex" />, false if not.
    /// </returns>
    ///
    /// <remarks>
	///	If all the vertices in <paramref name="vertices" /> can be reached from
	/// <paramref name="vertex" />, this method calculates the distance of the
	///	shortest path that reaches all the vertices, stores the distance at
	///	<paramref name="minimumDistance" />, and returns true.  Otherwise,
	///	false is returned.
    /// </remarks>
    //*************************************************************************

    Boolean
    CalculateMinimumDistance
    (
        IVertex vertex,
        IVertexCollection vertices,
		out Int32 minimumDistance
    );
}

}
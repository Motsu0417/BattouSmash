
//    MIT License
//    
//    Copyright (c) 2017 Dustin Whirle
//    
//    My Youtube stuff: https://www.youtube.com/playlist?list=PL-sp8pM7xzbVls1NovXqwgfBQiwhTA_Ya
//    
//    Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software without restriction, including without limitation the rights
//    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//    copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:
//    
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.
//    
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//    SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class MeshMaker
{
    //Meshの生成を行う・・・のかな？
    // Mesh Values
    //頂点座標リスト
    private List<Vector3> _vertices = new List<Vector3>();
    //法線ベクトルリスト
    private List<Vector3> _normals = new List<Vector3>();
    //UV（テクスチャ座標）リスト
    private List<Vector2> _uvs = new List<Vector2>();
    //接線リスト
    private List<Vector4> _tangents = new List<Vector4>();
    //<整数リスト>のリスト
    private List<List<int>> _subIndices = new List<List<int>>();

    public int VertCount
    {
        get
        {
            // 頂点リストの数をリターン
            return _vertices.Count;
        }
    }

    /// <summary>
    /// Clears all arrays
    /// </summary>
    public void Clear()
    {
        // 諸々の配列を初期化
        _vertices.Clear();
        _normals.Clear();
        _uvs.Clear();
        _tangents.Clear();
        _subIndices.Clear();
    }
    // AddTriangleに引数を展開して渡している
    public void AddTriangle(Triangle triangle, int submesh)
    {
        AddTriangle(triangle.vertices, triangle.uvs, triangle.normals, triangle.tangents, submesh);
    }

    /// <summary>
    /// Adds a new triangle to the return of GetMesh()
    /// </summary>
    /// <param name="vertices">Array of 3</param>
    /// <param name="normals">Array of 3</param>
    /// <param name="uvs">Array of 3</param>
    /// <param name="submesh">If you don't know put 0</param>
    public void AddTriangle(Vector3[] vertices, Vector2[] uvs, Vector3[] normals, int submesh = 0)
    {
        AddTriangle(vertices, uvs, normals, null, submesh);
    }
    /// <summary>
    /// Same as the first, but with tangents
    /// </summary>
    /// <param name="vertices">Array of 3</param>
    /// <param name="normals">Array of 3</param>
    /// <param name="uvs">Array of 3</param>
    /// <param name="tangents">Array of 3</param>
    /// <param name="submesh">If you don't know put 0</param>
    //　triangleをリストに追加
    public void AddTriangle(Vector3[] vertices, Vector2[] uvs, Vector3[] normals, Vector4[] tangents, int submesh = 0)
    {
        // 頂点座標リストのサイズを取得
        int vertCount = _vertices.Count;

        // 引数の三角形の頂点を頂点リストに移動
        _vertices.Add(vertices[0]);
        _vertices.Add(vertices[1]);
        _vertices.Add(vertices[2]);

        // 引数の三角形の法線を法線リストに移動
        _normals.Add(normals[0]);
        _normals.Add(normals[1]);
        _normals.Add(normals[2]);

        // 引数の三角形のuv座標をuv座標リストに移動
        _uvs.Add(uvs[0]);
        _uvs.Add(uvs[1]);
        _uvs.Add(uvs[2]);

        if (tangents != null)
        {
            // tangentsがあれば追加
            _tangents.Add(tangents[0]);
            _tangents.Add(tangents[1]);
            _tangents.Add(tangents[2]);
        }
        // 現状のポリゴン数と差があるなら、新たなポリゴンとして、インデックスリストを動的に追加
        if (_subIndices.Count < submesh + 1)
        {
            for (int i = _subIndices.Count; i < submesh + 1; i++)
            {
                _subIndices.Add(new List<int>());
            }
        }
        // ポリゴン毎に、頂点のインデックスを追加していく（頂点と対応）
        _subIndices[submesh].Add(vertCount);
        _subIndices[submesh].Add(vertCount + 1);
        _subIndices[submesh].Add(vertCount + 2);

    }
   
    ///// <summary>
    ///// Cleans up Double Vertices
    ///// </summary>
    //public void RemoveDoubles()
    //{

    //    int dubCount = 0;

    //    Vector3 vertex = Vector3.zero;
    //    Vector3 normal = Vector3.zero;
    //    Vector2 uv = Vector2.zero;
    //    Vector4 tangent = Vector4.zero;

    //    int iterator = 0;
    //    while (iterator < VertCount)
    //    {

    //        vertex = _vertices[iterator];
    //        normal = _normals[iterator];
    //        uv = _uvs[iterator];

    //        // look backwards for a match
    //        for (int backward_iterator = iterator - 1; backward_iterator >= 0; backward_iterator--)
    //        {

    //            if (vertex == _vertices[backward_iterator] &&
    //                normal == _normals[backward_iterator] &&
    //                uv == _uvs[backward_iterator])
    //            {
    //                dubCount++;
    //                DoubleFound(backward_iterator, iterator);
    //                iterator--;
    //                break; // there should only be one
    //            }
    //        }

    //        iterator++;

    //    } // while

    //    Debug.LogFormat("Doubles found {0}", dubCount);

    //}
    ///// <summary>
    ///// // go through all indices an replace them
    ///// </summary>
    ///// <param name="first"></param>
    ///// <param name="duplicate"></param>
    //private void DoubleFound(int first, int duplicate)
    //{
    //    for (int h = 0; h < _subIndices.Count; h++)
    //    {
    //        for (int i = 0; i < _subIndices[h].Count; i++)
    //        {

    //            if (_subIndices[h][i] > duplicate) // knock it down
    //                _subIndices[h][i]--;
    //            else if (_subIndices[h][i] == duplicate) // replace
    //                _subIndices[h][i] = first;
    //        }
    //    }

    //    _vertices.RemoveAt(duplicate);
    //    _normals.RemoveAt(duplicate);
    //    _uvs.RemoveAt(duplicate);

    //    if (_tangents.Count > 0)
    //        _tangents.RemoveAt(duplicate);

    //}

    /// <summary>
    /// Creates and returns a new mesh
    /// </summary>
    /// meshを作成してリターン
    public Mesh GetMesh()
    {
        // meshを作成
        Mesh shape = new Mesh();
        // 名前はGenerated Mesh
        shape.name = "Generated Mesh";
        // 頂点達を渡す
        shape.SetVertices(_vertices);
        // 法線たちを渡す
        shape.SetNormals(_normals);
        // uvを渡す
        shape.SetUVs(0, _uvs);
        shape.SetUVs(1, _uvs);

        // 接線リストが存在するなら渡す
        if (_tangents.Count > 1)
            shape.SetTangents(_tangents);

        // サブメッシュが存在するなら渡す
        shape.subMeshCount = _subIndices.Count;

        for (int i = 0; i < _subIndices.Count; i++)
            shape.SetTriangles(_subIndices[i], i);

        return shape;
    }
/*
#if UNITY_EDITOR
    /// <summary>
    /// Creates and returns a new mesh with generated lightmap uvs (Editor Only)
    /// </summary>
    public Mesh GetMesh_GenerateSecondaryUVSet()
    {

        Mesh shape = GetMesh();

        // for light mapping
        UnityEditor.Unwrapping.GenerateSecondaryUVSet(shape);

        return shape;
    }

    /// <summary>
    /// Creates and returns a new mesh with generated lightmap uvs (Editor Only)
    /// </summary>
    public Mesh GetMesh_GenerateSecondaryUVSet(UnityEditor.UnwrapParam param)
    {

        Mesh shape = GetMesh();

        // for light mapping
        UnityEditor.Unwrapping.GenerateSecondaryUVSet(shape, param);

        return shape;
    }
#endif
*/

    /// <summary>
    /// every property should have 3 elements 全ての頂点は少なくとも3つの要素を持っている
    /// </summary>
    public struct Triangle
    {
        //  頂点配列
        public Vector3[] vertices;
        //テクスチャ座標配列
        public Vector2[] uvs;
        //法線ベクトル配列
        public Vector3[] normals;
        //接線ベクトル配列
        public Vector4[] tangents;

        public Triangle(Vector3[] vertices = null, Vector2[] uvs = null, Vector3[] normals = null, Vector4[] tangents = null)
        {
            this.vertices = vertices;
            this.uvs = uvs;
            this.normals = normals;
            this.tangents = tangents;
        }
    }
}
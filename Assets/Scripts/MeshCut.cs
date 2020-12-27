
//    MIT License
//    
//    Copyright (c) 2017 Dustin Whirle
//    
//    My Yrefube stuff: https://www.yrefube.com/playlist?list=PL-sp8pM7xzbVls1NovXqwgfBQiwhTA_Ya
//    
//    Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software withref restriction, including withref limitation the rights
//    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//    copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:
//    
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.
//    
//    THE SOFTWARE IS PROVIDED "AS IS", WITHref WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//    ref OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//    SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MeshCut
{

    private static Plane blade;
    private static Mesh targetMesh;

    // 一時的に利用する変数達
    private static MeshMaker leftMeshMaker = new MeshMaker();
    private static MeshMaker rightMeshMaker = new MeshMaker();
    // Mesh_Maker内構造体Triangle
    // 三角形メッシュの座標・uv座標・法線・接線・
    private static MeshMaker.Triangle tmpTriangle = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    //
    private static List<Vector3> newVertices = new List<Vector3>();
    private static bool[] isLeftSides = new bool[3];
    private static int capMatSub = 1;

    // target = 切りたいオブジェクト
    // anchorPoint = 剣の座標
    // normalDirection = 剣の右方向ベクトル
    // capMaterial = 切った断面に被せるマテリアル
    // leftSideObject = 切った左半分のオブジェクト
    // rightSideObject = 切った右半分のオブジェクト
    public static void Cut(GameObject target, Vector3 anchorPoint, Vector3 normalDirection, Material capMaterial ,out GameObject leftSideObjct, out GameObject rightSideObjct)
    {

        // 不正な呼び出しを防ぐ
        if (target == null)
        {
            leftSideObjct = null;
            rightSideObjct = null;
            return;
        }

        // 切るプレートの定義(ブレードの左方向ベクトル,ブレードの座標)
        // 絶対座標・ベクトルから相対座標・ベクトルへと変換して引数を渡している
        blade = new Plane(target.transform.InverseTransformDirection(-normalDirection), target.transform.InverseTransformPoint(anchorPoint));

        // 切りたいオブジェクトのメッシュを取得
        targetMesh = target.GetComponent<MeshFilter>().mesh;

        //新しい2つのオブジェクト分のメッシュメーカーを初期化
        leftMeshMaker.Clear();
        rightMeshMaker.Clear();
        newVertices.Clear();

        int index_1, index_2, index_3;

        //Meshの頂点座標配列verticesを取得
        Vector3[] meshVertices = targetMesh.vertices;
        //Meshの法線ベクトル配列を取得
        Vector3[] meshNormals = targetMesh.normals;
        //MeshのUV(テクスチャを正方形に見たときの左端を0,0とみた座標)配列を取得
        Vector2[] meshUvs = targetMesh.uv;
        //tangents←接線ベクトルVector4
        Vector4[] meshTangents = targetMesh.tangents;

        //接線配列が存在するけどサイズが０のとき、接線配列を初期化
        if (meshTangents != null && meshTangents.Length == 0)
        {
            meshTangents = null;
        }

        // サブメッシュの数まで使う
        for (int c = 0; c < targetMesh.subMeshCount; c++)
        {
            // targetのメッシュのインデックスをindex配列に代入
            int[] index = targetMesh.GetTriangles(c);

            // インデックスを個々で見ていく
            for (int i = 0; i < index.Length; i += 3)
            {
                // ポリゴンの３頂点のインデックス
                index_1 = index[i]; //左下
                index_2 = index[i + 1]; //真ん中
                index_3 = index[i + 2]; //右上

                // メッシュのポリゴン一つ一つの情報を仮の三角形に
                // 3頂点の絶対座標
                tmpTriangle.vertices[0] = meshVertices[index_1];
                tmpTriangle.vertices[1] = meshVertices[index_2];
                tmpTriangle.vertices[2] = meshVertices[index_3];

                // 3頂点の法線ベクトル
                tmpTriangle.normals[0] = meshNormals[index_1];
                tmpTriangle.normals[1] = meshNormals[index_2];
                tmpTriangle.normals[2] = meshNormals[index_3];

                // 3頂点のUV
                tmpTriangle.uvs[0] = meshUvs[index_1];
                tmpTriangle.uvs[1] = meshUvs[index_2];
                tmpTriangle.uvs[2] = meshUvs[index_3];
               
                // tangentsは接線ベクトル
                if (meshTangents != null)
                {
                    // 接線ベクトルが存在する場合
                    tmpTriangle.tangents[0] = meshTangents[index_1];
                    tmpTriangle.tangents[1] = meshTangents[index_2];
                    tmpTriangle.tangents[2] = meshTangents[index_3];
                }
                else
                {
                    // もし接線ベクトルが存在しなければ、zeroベクトルを設定
                    tmpTriangle.tangents[0] = Vector4.zero;
                    tmpTriangle.tangents[1] = Vector4.zero;
                    tmpTriangle.tangents[2] = Vector4.zero;
                }

                /* ポリゴンの頂点たちについて 
                 * ポリゴンの頂点には4状態ある。
                 * ①全ての頂点がブレードの左（ブレードの表）側
                 * ②全ての頂点がブレードの左側
                 * ③3つの頂点のうち、1つの頂点が右側
                 * ④3つの頂点のうち、2つの頂点が右側
                 */

                // メッシュの頂点が、ブレードのおもて（法線ベクトル側）にあるかチェック
                // 法線ベクトル側（左側）にあればtrue,出なければfalse
                isLeftSides[0] = blade.GetSide(meshVertices[index_1]);
                isLeftSides[1] = blade.GetSide(meshVertices[index_2]);
                isLeftSides[2] = blade.GetSide(meshVertices[index_3]);

                // ①・② 全ての頂点がどちらか片方に偏っている＝状態が同じ
                if (isLeftSides[0] == isLeftSides[1] && isLeftSides[0] == isLeftSides[2])
                {
                    // ①の状態の時、左側のリストに追加する
                    if (isLeftSides[0]) // left side
                        // 左側三角形（ポリゴン）リストに今対象のポリゴンを追加
                        leftMeshMaker.AddTriangle(tmpTriangle, c);
                    else // ②の状態の時、
                        // 右側三角形（ポリゴン）リストに今対象のポリゴンを追加
                        rightMeshMaker.AddTriangle(tmpTriangle, c);
                }
                else // どれか一つが反対側にある場合
                { 
                    // 三角形(ポリゴン)を切るスプリクトを実行
                    Cut_this_Face(ref tmpTriangle, c);
                }
            }
        }

        // 断面に被せるメッシュの準備
        // mats = ターゲットのマテリアル
        Material[] mats = target.GetComponent<MeshRenderer>().sharedMaterials;
        // メッシュのマテリアルが被せるマテリアルと同じならパス
        // 違うならマテリアルを追加
        if (mats[mats.Length - 1].name != capMaterial.name)
        {
            // matsより１つ大きな配列newMatsを作成
            Material[] newMats = new Material[mats.Length + 1];
            // matsの中身をnewMatsにコピー
            mats.CopyTo(newMats, 0);
            // newMatsの最後[mats.length]には断面のマテリアルを追加
            newMats[mats.Length] = capMaterial;
            // matsに新しく追加したnewMatsを代入する
            mats = newMats;
        }

        // 被せるマテリアルの添え字を取得
        capMatSub = mats.Length - 1;

        // 切断処理
        Cap_the_Cut();

        // leftSideに入っているポリゴン達をメッシュにまとめる
        Mesh leftSideMesh = leftMeshMaker.GetMesh();
        leftSideMesh.name = "Split Mesh Left";

        //　同様に右側のポリゴン達もメッシュにまとめる
        Mesh rightSideMesh = rightMeshMaker.GetMesh();
        rightSideMesh.name = "Split Mesh Right";

        //準備ができたので、実際に2つのオブジェクトを作成していく

        // 左側のオブジェクトを作成
        leftSideObjct = new GameObject("LeftSide", typeof(MeshFilter), typeof(MeshRenderer), typeof(Rigidbody));
        leftSideObjct.transform.position = target.transform.position;
        leftSideObjct.transform.rotation = target.transform.rotation;
        leftSideObjct.GetComponent<MeshFilter>().mesh = leftSideMesh;

        // 右側のオブジェクトを作成
        rightSideObjct = new GameObject("RightSide", typeof(MeshFilter), typeof(MeshRenderer),typeof(Rigidbody));
        rightSideObjct.transform.position = target.transform.position;
        rightSideObjct.transform.rotation = target.transform.rotation;
        rightSideObjct.GetComponent<MeshFilter>().mesh = rightSideMesh;

        // 切ったブジェクトにRigidbodyを付加する
        //if (!rightSideObj.GetComponent<Rigidbody>()) rightSideObj.AddComponent<Rigidbody>();
        if (!leftSideObjct.GetComponent<Rigidbody>()) leftSideObjct.AddComponent<Rigidbody>();

        // 切ったオブジェクトを分けるために切れた方向に少しずらす
        //rightSideObjct.transform.position += -_blade.normal * 0.01f;
        //leftSideObjct.transform.position += _blade.normal * 0.01f;
        rightSideObjct.GetComponent<Rigidbody>().AddForce(-blade.normal * 50f);
        leftSideObjct.GetComponent<Rigidbody>().AddForce(blade.normal * 50f);


        // もしターゲットのオブジェクトに親がいれば、入れ子に
        if (target.transform.parent != null)
        {
            leftSideObjct.transform.parent = target.transform.parent;
            rightSideObjct.transform.parent = target.transform.parent;
        }
        // スケールをコピー
        leftSideObjct.transform.localScale = target.transform.localScale;
        rightSideObjct.transform.localScale = target.transform.localScale;

        // どちらとものマテリアルをmatsに
        leftSideObjct.GetComponent<MeshRenderer>().materials = mats;
        rightSideObjct.GetComponent<MeshRenderer>().materials = mats;
    }

    #region Cutting
    // Caching
    private static MeshMaker.Triangle _leftTriangleCache = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    private static MeshMaker.Triangle _rightTriangleCache = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    private static MeshMaker.Triangle _newTriangleCache = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    // Functions
    private static void Cut_this_Face(ref MeshMaker.Triangle triangle, int submesh)
    {
        // ブレードの表(左側)にあればtrue,違うならfalse
        isLeftSides[0] = blade.GetSide(triangle.vertices[0]); // true = left
        isLeftSides[1] = blade.GetSide(triangle.vertices[1]);
        isLeftSides[2] = blade.GetSide(triangle.vertices[2]);


        int leftCount = 0;
        int rightCount = 0;

        for (int i = 0; i < 3; i++)
        {
            // もし頂点が左側なら
            if (isLeftSides[i])
            { // left
                // 左側頂点リストに追加
                _leftTriangleCache.vertices[leftCount] = triangle.vertices[i];
                _leftTriangleCache.uvs[leftCount] = triangle.uvs[i];
                _leftTriangleCache.normals[leftCount] = triangle.normals[i];
                _leftTriangleCache.tangents[leftCount] = triangle.tangents[i];
                // 左側頂点カウントを +1
                leftCount++;
            }
            else // それ以外（頂点が右側）なら
            { // right
                // 右側頂点リストに追加
                _rightTriangleCache.vertices[rightCount] = triangle.vertices[i];
                _rightTriangleCache.uvs[rightCount] = triangle.uvs[i];
                _rightTriangleCache.normals[rightCount] = triangle.normals[i];
                _rightTriangleCache.tangents[rightCount] = triangle.tangents[i];
                // 右側頂点カウントを +1
                rightCount++;
            }
        }

        // find the new triangles X 3
        // first the new vertices

        // this will give me a triangle with the solo point as first
        // 頂点の割合が1:2（左側が１）の時
        // 左側の頂点を始点としたトライアングルを生成
        if (leftCount == 1)
        {
            tmpTriangle.vertices[0] = _leftTriangleCache.vertices[0];
            tmpTriangle.uvs[0] = _leftTriangleCache.uvs[0];
            tmpTriangle.normals[0] = _leftTriangleCache.normals[0];
            tmpTriangle.tangents[0] = _leftTriangleCache.tangents[0];

            tmpTriangle.vertices[1] = _rightTriangleCache.vertices[0];
            tmpTriangle.uvs[1] = _rightTriangleCache.uvs[0];
            tmpTriangle.normals[1] = _rightTriangleCache.normals[0];
            tmpTriangle.tangents[1] = _rightTriangleCache.tangents[0];

            tmpTriangle.vertices[2] = _rightTriangleCache.vertices[1];
            tmpTriangle.uvs[2] = _rightTriangleCache.uvs[1];
            tmpTriangle.normals[2] = _rightTriangleCache.normals[1];
            tmpTriangle.tangents[2] = _rightTriangleCache.tangents[1];
        }
        else  
        {
            // 頂点の割合が2:1（左側が2）の時
            // 右側の頂点を始点としたトライアングルを生成

            tmpTriangle.vertices[0] = _rightTriangleCache.vertices[0];
            tmpTriangle.uvs[0] = _rightTriangleCache.uvs[0];
            tmpTriangle.normals[0] = _rightTriangleCache.normals[0];
            tmpTriangle.tangents[0] = _rightTriangleCache.tangents[0];

            tmpTriangle.vertices[1] = _leftTriangleCache.vertices[0];
            tmpTriangle.uvs[1] = _leftTriangleCache.uvs[0];
            tmpTriangle.normals[1] = _leftTriangleCache.normals[0];
            tmpTriangle.tangents[1] = _leftTriangleCache.tangents[0];

            tmpTriangle.vertices[2] = _leftTriangleCache.vertices[1];
            tmpTriangle.uvs[2] = _leftTriangleCache.uvs[1];
            tmpTriangle.normals[2] = _leftTriangleCache.normals[1];
            tmpTriangle.tangents[2] = _leftTriangleCache.tangents[1];
        }

        // now to find the intersection points between the solo point and the others
        // 1つ側の頂点から、残り2点までの間にあるブレードの交点を求める
        float distance = 0;
        float normalizedDistance = 0.0f;
        Vector3 edgeVector = Vector3.zero; // contains edge length and direction

        // 頂点０から頂点1までのベクトルをedgeVectorへ
        edgeVector = tmpTriangle.vertices[1] - tmpTriangle.vertices[0];
        // Raycast(頂点0からedgeVector（単位ベクトルへ変換）に向けて)レイを照射、距離を取得
        blade.Raycast(new Ray(tmpTriangle.vertices[0], edgeVector.normalized), out distance);

        // edgeVector.magunitude = ベクトルの距離
        // normalizedDistance = ベクトルの距離の比（頂点0から頂点１までの距離を1とした時のブレードまでの距離）
        normalizedDistance = distance / edgeVector.magnitude;
        // lerp（点１,点2,割合）で、頂点0から頂点１までのベクトルをnormalizedDistanceの割合で作成する
        _newTriangleCache.vertices[0] = Vector3.Lerp(tmpTriangle.vertices[0], tmpTriangle.vertices[1], normalizedDistance);
        // ↑のuvバージョン
        _newTriangleCache.uvs[0] = Vector2.Lerp(tmpTriangle.uvs[0], tmpTriangle.uvs[1], normalizedDistance);
        // ↑の法線ベクトルバージョン
        _newTriangleCache.normals[0] = Vector3.Lerp(tmpTriangle.normals[0], tmpTriangle.normals[1], normalizedDistance);
        // ↑の接線ベクトルバージョン
        _newTriangleCache.tangents[0] = Vector4.Lerp(tmpTriangle.tangents[0], tmpTriangle.tangents[1], normalizedDistance);

        // さっきの処理を頂点0と頂点2で行う
        edgeVector = tmpTriangle.vertices[2] - tmpTriangle.vertices[0];
        blade.Raycast(new Ray(tmpTriangle.vertices[0], edgeVector.normalized), out distance);

        normalizedDistance = distance / edgeVector.magnitude;
        _newTriangleCache.vertices[1] = Vector3.Lerp(tmpTriangle.vertices[0], tmpTriangle.vertices[2], normalizedDistance);
        _newTriangleCache.uvs[1] = Vector2.Lerp(tmpTriangle.uvs[0], tmpTriangle.uvs[2], normalizedDistance);
        _newTriangleCache.normals[1] = Vector3.Lerp(tmpTriangle.normals[0], tmpTriangle.normals[2], normalizedDistance);
        _newTriangleCache.tangents[1] = Vector4.Lerp(tmpTriangle.tangents[0], tmpTriangle.tangents[2], normalizedDistance);

        // 新しく作った頂点がお互い異なるなら成功
        if (_newTriangleCache.vertices[0] != _newTriangleCache.vertices[1])
        {
            //新しい頂点を新ポリゴンリストに追加
            //tracking newly created points
            newVertices.Add(_newTriangleCache.vertices[0]);
            newVertices.Add(_newTriangleCache.vertices[1]);
        }
        // make the new triangles
        // one side will get 1 the other will get 2
        // 頂点の割合が1:2（左側が１）の時
        if (leftCount == 1)
        {
            // first one on the left
            // ブレードの左部分ポリゴン
            tmpTriangle.vertices[0] = _leftTriangleCache.vertices[0];
            tmpTriangle.uvs[0] = _leftTriangleCache.uvs[0];
            tmpTriangle.normals[0] = _leftTriangleCache.normals[0];
            tmpTriangle.tangents[0] = _leftTriangleCache.tangents[0];

            tmpTriangle.vertices[1] = _newTriangleCache.vertices[0];
            tmpTriangle.uvs[1] = _newTriangleCache.uvs[0];
            tmpTriangle.normals[1] = _newTriangleCache.normals[0];
            tmpTriangle.tangents[1] = _newTriangleCache.tangents[0];

            tmpTriangle.vertices[2] = _newTriangleCache.vertices[1];
            tmpTriangle.uvs[2] = _newTriangleCache.uvs[1];
            tmpTriangle.normals[2] = _newTriangleCache.normals[1];
            tmpTriangle.tangents[2] = _newTriangleCache.tangents[1];

            // check if it is facing the right way
            // ポリゴンの表面が裏を向いていないかチェック
            NormalCheck(ref tmpTriangle);

            // add it
            //ポリゴンを左側ポリゴンリストに追加
            leftMeshMaker.AddTriangle(tmpTriangle, submesh);

 
            // other two on the right
            // 右側の4角形を3角形のポリゴン2つにする。

            //一つ目
            tmpTriangle.vertices[0] = _rightTriangleCache.vertices[0];
            tmpTriangle.uvs[0] = _rightTriangleCache.uvs[0];
            tmpTriangle.normals[0] = _rightTriangleCache.normals[0];
            tmpTriangle.tangents[0] = _rightTriangleCache.tangents[0];

            tmpTriangle.vertices[1] = _newTriangleCache.vertices[0];
            tmpTriangle.uvs[1] = _newTriangleCache.uvs[0];
            tmpTriangle.normals[1] = _newTriangleCache.normals[0];
            tmpTriangle.tangents[1] = _newTriangleCache.tangents[0];

            tmpTriangle.vertices[2] = _newTriangleCache.vertices[1];
            tmpTriangle.uvs[2] = _newTriangleCache.uvs[1];
            tmpTriangle.normals[2] = _newTriangleCache.normals[1];
            tmpTriangle.tangents[2] = _newTriangleCache.tangents[1];

            // check if it is facing the right way
            NormalCheck(ref tmpTriangle);

            // add it
            rightMeshMaker.AddTriangle(tmpTriangle, submesh);

            // third
            // 右側2つ目
            tmpTriangle.vertices[0] = _rightTriangleCache.vertices[0];
            tmpTriangle.uvs[0] = _rightTriangleCache.uvs[0];
            tmpTriangle.normals[0] = _rightTriangleCache.normals[0];
            tmpTriangle.tangents[0] = _rightTriangleCache.tangents[0];

            tmpTriangle.vertices[1] = _rightTriangleCache.vertices[1];
            tmpTriangle.uvs[1] = _rightTriangleCache.uvs[1];
            tmpTriangle.normals[1] = _rightTriangleCache.normals[1];
            tmpTriangle.tangents[1] = _rightTriangleCache.tangents[1];

            tmpTriangle.vertices[2] = _newTriangleCache.vertices[1];
            tmpTriangle.uvs[2] = _newTriangleCache.uvs[1];
            tmpTriangle.normals[2] = _newTriangleCache.normals[1];
            tmpTriangle.tangents[2] = _newTriangleCache.tangents[1];

            // check if it is facing the right way
            NormalCheck(ref tmpTriangle);

            // add it
            rightMeshMaker.AddTriangle(tmpTriangle, submesh);
        }
        else //上の処理の左右逆パターン
        {
            // first one on the right
            tmpTriangle.vertices[0] = _rightTriangleCache.vertices[0];
            tmpTriangle.uvs[0] = _rightTriangleCache.uvs[0];
            tmpTriangle.normals[0] = _rightTriangleCache.normals[0];
            tmpTriangle.tangents[0] = _rightTriangleCache.tangents[0];

            tmpTriangle.vertices[1] = _newTriangleCache.vertices[0];
            tmpTriangle.uvs[1] = _newTriangleCache.uvs[0];
            tmpTriangle.normals[1] = _newTriangleCache.normals[0];
            tmpTriangle.tangents[1] = _newTriangleCache.tangents[0];

            tmpTriangle.vertices[2] = _newTriangleCache.vertices[1];
            tmpTriangle.uvs[2] = _newTriangleCache.uvs[1];
            tmpTriangle.normals[2] = _newTriangleCache.normals[1];
            tmpTriangle.tangents[2] = _newTriangleCache.tangents[1];

            // check if it is facing the right way
            NormalCheck(ref tmpTriangle);

            // add it
            rightMeshMaker.AddTriangle(tmpTriangle, submesh);


            // other two on the left
            tmpTriangle.vertices[0] = _leftTriangleCache.vertices[0];
            tmpTriangle.uvs[0] = _leftTriangleCache.uvs[0];
            tmpTriangle.normals[0] = _leftTriangleCache.normals[0];
            tmpTriangle.tangents[0] = _leftTriangleCache.tangents[0];

            tmpTriangle.vertices[1] = _newTriangleCache.vertices[0];
            tmpTriangle.uvs[1] = _newTriangleCache.uvs[0];
            tmpTriangle.normals[1] = _newTriangleCache.normals[0];
            tmpTriangle.tangents[1] = _newTriangleCache.tangents[0];

            tmpTriangle.vertices[2] = _newTriangleCache.vertices[1];
            tmpTriangle.uvs[2] = _newTriangleCache.uvs[1];
            tmpTriangle.normals[2] = _newTriangleCache.normals[1];
            tmpTriangle.tangents[2] = _newTriangleCache.tangents[1];

            // check if it is facing the right way
            NormalCheck(ref tmpTriangle);

            // add it
            leftMeshMaker.AddTriangle(tmpTriangle, submesh);

            // third
            tmpTriangle.vertices[0] = _leftTriangleCache.vertices[0];
            tmpTriangle.uvs[0] = _leftTriangleCache.uvs[0];
            tmpTriangle.normals[0] = _leftTriangleCache.normals[0];
            tmpTriangle.tangents[0] = _leftTriangleCache.tangents[0];

            tmpTriangle.vertices[1] = _leftTriangleCache.vertices[1];
            tmpTriangle.uvs[1] = _leftTriangleCache.uvs[1];
            tmpTriangle.normals[1] = _leftTriangleCache.normals[1];
            tmpTriangle.tangents[1] = _leftTriangleCache.tangents[1];

            tmpTriangle.vertices[2] = _newTriangleCache.vertices[1];
            tmpTriangle.uvs[2] = _newTriangleCache.uvs[1];
            tmpTriangle.normals[2] = _newTriangleCache.normals[1];
            tmpTriangle.tangents[2] = _newTriangleCache.tangents[1];

            // check if it is facing the right way
            NormalCheck(ref tmpTriangle);

            // add it
            leftMeshMaker.AddTriangle(tmpTriangle, submesh);
        }

    }
    #endregion

    #region Capping
    // 断面にメッシュを作る
    // Caching
    private static List<int> _capUsedIndicesCache = new List<int>();
    private static List<int> _capPolygonIndicesCache = new List<int>();
    // Functions
    private static void Cap_the_Cut()
    {

        _capUsedIndicesCache.Clear();
        _capPolygonIndicesCache.Clear();

        // find the needed polygons
        // the cut faces added new vertices by 2 each time to make an edge
        // if two edges contain the same Vector3 point, they are connected
        // 必要なポリゴンを見つける
        // 断面に2つの角を作るための頂点を追加する（？）
        // もし2点の座標が同じなら、それらは結合している

        // 新しく生成された頂点の数繰り返す
        // += 2なのは、頂点は2つずつ生成されているため
        for (int i = 0; i < newVertices.Count; i += 2)
        {
            // check the edge
            // iが調査済みの場合スキップ
            if (_capUsedIndicesCache.Contains(i)) continue; // if it has one, it has this edge

            //new polygon started with this edge
            _capPolygonIndicesCache.Clear();
            // i,i+1をインデックスとしてリストに追加
            _capPolygonIndicesCache.Add(i);
            _capPolygonIndicesCache.Add(i + 1);

            // リストに追加したので、使用済みのフラッグをつける
            _capUsedIndicesCache.Add(i);
            _capUsedIndicesCache.Add(i + 1);

            // 頂点座標を変数に代入
            Vector3 connectionPointLeft = newVertices[i];
            Vector3 connectionPointRight = newVertices[i + 1];
            // 完了していたらtrue
            bool isDone = false;

            // look for more edges もっと縁を見つける
            // 重複している頂点がなくなるまで
            while (!isDone)
            {
                isDone = true;

                // loop through edges
                // 新しく生成された頂点全てに対して実行 += 2
                for (int index = i+2; index < newVertices.Count; index += 2)
                {   
                    // if it has one, it has this edge
                    // indexがusedの時はスキップ
                    if (_capUsedIndicesCache.Contains(index)) continue;

                    Vector3 nextPoint1 = newVertices[index];
                    Vector3 nextPoint2 = newVertices[index + 1];

                    // check for next point in the chain
                    //connectionPointLeft,Right が　nextPoint1,2と重複していたら
                    if (connectionPointLeft == nextPoint1 ||
                        connectionPointLeft == nextPoint2 ||
                        connectionPointRight == nextPoint1 ||
                        connectionPointRight == nextPoint2)
                    {
                        // index,+1をusedの配列に代入する
                        _capUsedIndicesCache.Add(index);
                        _capUsedIndicesCache.Add(index + 1);

                        // add the other
                        // 重複していたら
                        if (connectionPointLeft == nextPoint1)
                        {
                            // index+1の数字 をポリゴンインデックス配列の先頭に挿入
                            _capPolygonIndicesCache.Insert(0, index + 1);
                            // 左接続ポイントにnextPoint2を上書き
                            connectionPointLeft = nextPoint2;
                        }
                        else if (connectionPointLeft == nextPoint2)
                        {   // 重複していたら
                            // indexの数字をポリゴンインデックス配列の先頭に挿入
                            _capPolygonIndicesCache.Insert(0, index);
                            // 左接続ポイントにnextPoint1を上書き
                            connectionPointLeft = nextPoint1;
                        }
                        else if (connectionPointRight == nextPoint1)
                        {   // 重複していたら
                            // index+1の数字をポリゴンインデックス配列の先頭に挿入
                            _capPolygonIndicesCache.Add(index + 1);
                            // 右接続ポイントにnextPoint2を上書き
                            connectionPointRight = nextPoint2;
                        }
                        else if (connectionPointRight == nextPoint2)
                        {   // 重複していたら
                            _capPolygonIndicesCache.Add(index);
                            // 右接続ポイントにnextPoint1を上書き
                            connectionPointRight = nextPoint1;
                        }
                        // 次もループを行う
                        isDone = false;
                    }
                }
            }// while isDone = False

            // check if the link is closed
            // first == last
            // _capPolygonIndecesCacheの最初と最後の座標が同じなら、_capPolygonIndecesCacheの最後のインデックスに最初のインデックスを代入する
            if (newVertices[_capPolygonIndicesCache[0]] == newVertices[_capPolygonIndicesCache[_capPolygonIndicesCache.Count - 1]])
                _capPolygonIndicesCache[_capPolygonIndicesCache.Count - 1] = _capPolygonIndicesCache[0];
            else
                _capPolygonIndicesCache.Add(_capPolygonIndicesCache[0]); // そうでない場合、最初のインデックスを末尾に追加する

            // cap
            // 1ポリゴンずつふさいでいく
            // ポリゴンのインデックスを渡す
            FillCap_Method1(_capPolygonIndicesCache);
        }
    }
    private static void FillCap_Method1(List<int> indices)
    {

        // center of the cap
        // 頂点を足して数で割り、ポリゴンの重心を求めている
        Vector3 center = Vector3.zero;
        // foreachでインデックスごとに座標を足す
        foreach (var index in indices)
            center += newVertices[index];
        // 足した座標を数で割って重心を出す
        center = center / indices.Count;
        
        // bladeの法線ベクトルをz軸に90°傾けたupwardを取得
        Vector3 upward = Vector3.zero;
        upward.x = blade.normal.y;
        upward.y = -blade.normal.x;
        upward.z = blade.normal.z;

        //Debug.DrawRay(Camera.main.ScreenPointToRay(Input.mousePosition).origin, _blade.normal * 10, Color.blue, 10);

        //ブレードの法線とupwardの外積を求め、横軸を取得する
        Vector3 left = Vector3.Cross(blade.normal, upward);

        Vector3 displacement = Vector3.zero;
        Vector2 newUV1 = Vector2.zero;
        Vector2 newUV2 = Vector2.zero;
        Vector2 newUV3 = Vector2.zero;

        // indices should be in order like a closed chain

        // go through edges and eliminate by creating triangles with connected edges
        // each new triangle removes 2 edges but creates 1 new edge
        // keep the chain in order

        int iterator = 0;
        // 大体インデックスのカウントって2超えない？
        // 四角の場合も対応している？
        while (indices.Count > 2)
        {
            // 新しい3角形の頂点をlinkに代入,3以上受け取る事を予想して、あまりで取得している
            Vector3 link1 = newVertices[indices[iterator]];
            Vector3 link2 = newVertices[indices[(iterator + 1) % indices.Count]];
            Vector3 link3 = newVertices[indices[(iterator + 2) % indices.Count]];

            // 中心からlink方向へのベクトル
            displacement = link1 - center;
            newUV1 = Vector3.zero;
            // uvの中心である0.5f,0.5fに内積を足しuv座標を求める
            newUV1.x = 0.5f + Vector3.Dot(displacement, left);
            newUV1.y = 0.5f + Vector3.Dot(displacement, upward);
            //newUV1.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

            // 中心からlink方向へのベクトル
            displacement = link2 - center;
            newUV2 = Vector3.zero;
            newUV2.x = 0.5f + Vector3.Dot(displacement, left);
            newUV2.y = 0.5f + Vector3.Dot(displacement, upward);
            //newUV2.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

            // 中心からlink方向へのベクトル
            displacement = link3 - center;
            newUV3 = Vector3.zero;
            newUV3.x = 0.5f + Vector3.Dot(displacement, left);
            newUV3.y = 0.5f + Vector3.Dot(displacement, upward);
            //newUV2.z = 0.5f + Vector3.Dot(displacement, _blade.normal);


            // add triangle
            // 今求めたデータを元に3角形を構成する
            _newTriangleCache.vertices[0] = link1;
            _newTriangleCache.uvs[0] = newUV1;
            _newTriangleCache.normals[0] = -blade.normal;
            _newTriangleCache.tangents[0] = Vector4.zero;

            _newTriangleCache.vertices[1] = link2;
            _newTriangleCache.uvs[1] = newUV2;
            _newTriangleCache.normals[1] = -blade.normal;
            _newTriangleCache.tangents[1] = Vector4.zero;

            _newTriangleCache.vertices[2] = link3;
            _newTriangleCache.uvs[2] = newUV3;
            _newTriangleCache.normals[2] = -blade.normal;
            _newTriangleCache.tangents[2] = Vector4.zero;

            // add to left side
            // 三角形の向きを調整する
            NormalCheck(ref _newTriangleCache);

            // 左側に3角形を追加
            leftMeshMaker.AddTriangle(_newTriangleCache, capMatSub);

            // add to right side
            //同様の位置に反対向きのポリゴンも生成
            _newTriangleCache.normals[0] = blade.normal;
            _newTriangleCache.normals[1] = blade.normal;
            _newTriangleCache.normals[2] = blade.normal;

            NormalCheck(ref _newTriangleCache);

            // 右側のポリゴン群に追加
            rightMeshMaker.AddTriangle(_newTriangleCache, capMatSub);

            // adjust indices by removing the middle link
            // ベクトルの真ん中を削除する
            indices.RemoveAt((iterator + 1) % indices.Count);

            // move on
            // イテレーターを+1（indexのサイズ以下に修正しつつ）
            iterator = (iterator + 1) % indices.Count;
        }

    }
    /*
    private static void FillCap_Method2(List<int> indices)
    {

        // center of the cap
        Vector3 center = Vector3.zero;
        foreach (var index in indices)
            center += _newVerticesCache[index];

        center = center / indices.Count;

        // you need an axis based on the cap
        Vector3 upward = Vector3.zero;
        // 90 degree turn
        upward.x = _blade.normal.y;
        upward.y = -_blade.normal.x;
        upward.z = _blade.normal.z;
        Vector3 left = Vector3.Cross(_blade.normal, upward);

        Vector3 displacement = Vector3.zero;
        Vector2 newUV1 = Vector2.zero;
        Vector2 newUV2 = Vector2.zero;

        for (int i = 0; i < indices.Count - 1; i++)
        {

            displacement = _newVerticesCache[indices[i]] - center;
            newUV1 = Vector3.zero;
            newUV1.x = 0.5f + Vector3.Dot(displacement, left);
            newUV1.y = 0.5f + Vector3.Dot(displacement, upward);
            //newUV1.z = 0.5f + Vector3.Dot(displacement, _blade.normal);

            displacement = _newVerticesCache[indices[i + 1]] - center;
            newUV2 = Vector3.zero;
            newUV2.x = 0.5f + Vector3.Dot(displacement, left);
            newUV2.y = 0.5f + Vector3.Dot(displacement, upward);
            //newUV2.z = 0.5f + Vector3.Dot(displacement, _blade.normal);



            _newTriangleCache.vertices[0] = _newVerticesCache[indices[i]];
            _newTriangleCache.uvs[0] = newUV1;
            _newTriangleCache.normals[0] = -_blade.normal;
            _newTriangleCache.tangents[0] = Vector4.zero;

            _newTriangleCache.vertices[1] = _newVerticesCache[indices[i + 1]];
            _newTriangleCache.uvs[1] = newUV2;
            _newTriangleCache.normals[1] = -_blade.normal;
            _newTriangleCache.tangents[1] = Vector4.zero;

            _newTriangleCache.vertices[2] = center;
            _newTriangleCache.uvs[2] = new Vector2(0.5f, 0.5f);
            _newTriangleCache.normals[2] = -_blade.normal;
            _newTriangleCache.tangents[2] = Vector4.zero;


            NormalCheck(ref _newTriangleCache);

            _leftSide.AddTriangle(_newTriangleCache, _capMatSub);

            _newTriangleCache.normals[0] = _blade.normal;
            _newTriangleCache.normals[1] = _blade.normal;
            _newTriangleCache.normals[2] = _blade.normal;

            NormalCheck(ref _newTriangleCache);

            _rightSide.AddTriangle(_newTriangleCache, _capMatSub);

        }
    }
    */
    #endregion

    #region Misc.
    //法線のチェック（ポリゴンの向きをチェック）
    private static void NormalCheck(ref MeshMaker.Triangle triangle)
    {
        // 2ベクトルの法線ベクトル
        // ポリゴンの法線ベクトルの算出
        Vector3 crossProduct = Vector3.Cross(triangle.vertices[1] - triangle.vertices[0], triangle.vertices[2] - triangle.vertices[0]);
        // 3点の重心から出る法線ベクトル
        Vector3 averageNormal = (triangle.normals[0] + triangle.normals[1] + triangle.normals[2]) / 3.0f;
        // 2ベクトル間の内積を求める
        float dotProduct = Vector3.Dot(averageNormal, crossProduct);
        // 内積が負＝面の向きが違う
        if (dotProduct < 0)
        {
            // 3点の始点0と終点2を入れ替えて、表裏を入れ替える
            Vector3 temp = triangle.vertices[2];
            triangle.vertices[2] = triangle.vertices[0];
            triangle.vertices[0] = temp;

            temp = triangle.normals[2];
            triangle.normals[2] = triangle.normals[0];
            triangle.normals[0] = temp;

            Vector2 temp2 = triangle.uvs[2];
            triangle.uvs[2] = triangle.uvs[0];
            triangle.uvs[0] = temp2;

            Vector4 temp3 = triangle.tangents[2];
            triangle.tangents[2] = triangle.tangents[0];
            triangle.tangents[0] = temp3;
        }

    }
    #endregion
}
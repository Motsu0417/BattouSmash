
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

using System.Collections.Generic;
using UnityEngine;

public class MeshCut
{
    // 切断面の板（プレーン）
    private static Plane blade;
    // 切断するtargetのメッシュ
    private static Mesh targetMesh;
    // 一時的に利用する変数達
    private static MeshMaker leftMeshMaker = new MeshMaker();
    private static MeshMaker rightMeshMaker = new MeshMaker();
    // Mesh_Maker内構造体Triangle
    // 三角形メッシュの頂点の座標・uv座標・法線・接線
    private static MeshMaker.Triangle tmpTriangle = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    // 三角形の頂点の座標を入れるリスト
    private static List<Vector3> newVertices = new List<Vector3>();
    // 各頂点が切断面に対して表か裏かを保存する配列
    private static bool[] isLeftSides = new bool[3];
    // 切るオブジェクトのサブメッシュの数を保存する変数、初期値は１
    private static int capMatSub = 1;

    /**
     * target = 切りたいオブジェクト
     * anchorPoint = 剣の座標
     * normalDirection = 剣の右方向ベクトル
     * capMaterial = 切った断面に被せるマテリアル
     * leftSideObject = 切った左半分のオブジェクト
     * rightSideObject = 切った右半分のオブジェクト
    */
    public static void Cut(GameObject target, Vector3 anchorPoint, Vector3 normalDirection, Material capMaterial ,out GameObject leftSideObjct, out GameObject rightSideObjct)
    {
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

                /** ポリゴンの頂点たちについて 
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

        //準備ができたので、実際に2つのオブジェクトを作成していく-------------------------------------------------------------
        
        // オブジェクトを新しく作り、MeshFilter,MeshRenderer,Rigidbodyを付ける
        // 左右のオブジェクトを作成
        leftSideObjct = new GameObject("LeftSide", typeof(MeshFilter), typeof(MeshRenderer), typeof(Rigidbody));
        rightSideObjct = new GameObject("RightSide", typeof(MeshFilter), typeof(MeshRenderer),typeof(Rigidbody));
        
        // 位置・傾きを元オブジェクトからコピー
        leftSideObjct.transform.position = rightSideObjct.transform.position = target.transform.position;
        leftSideObjct.transform.rotation = rightSideObjct.transform.rotation = target.transform.rotation;
        
        // メッシュをそれぞれ適用する
        leftSideObjct.GetComponent<MeshFilter>().mesh = leftSideMesh;
        rightSideObjct.GetComponent<MeshFilter>().mesh = rightSideMesh;
        
        // 当たり判定をつける
        // RigidbodyとMeshColliderを共存させるときは、convexをtrueにする
        leftSideObjct.AddComponent<MeshCollider>().convex = true;
        rightSideObjct.AddComponent<MeshCollider>().convex = true;
        
        // 切ったオブジェクトを分けるために切れた方向に少しずらす
        rightSideObjct.GetComponent<Rigidbody>().AddForce(-blade.normal * 50f);
        leftSideObjct.GetComponent<Rigidbody>().AddForce(blade.normal * 50f);
        
        // もしターゲットのオブジェクトに親がいれば子にする
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
    private static MeshMaker.Triangle leftTriangle = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    private static MeshMaker.Triangle rightTriangle = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    private static MeshMaker.Triangle newTriangle = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    
    private static void Cut_this_Face(ref MeshMaker.Triangle triangle, int submesh)
    {
        // ブレードの表(左側)にあればtrue,違うならfalse
        isLeftSides[0] = blade.GetSide(triangle.vertices[0]); // true = left
        isLeftSides[1] = blade.GetSide(triangle.vertices[1]);
        isLeftSides[2] = blade.GetSide(triangle.vertices[2]);

        // 面に対する位置ごとに各頂点の数を記録する変数
        int leftCount = 0;
        int rightCount = 0;

        for (int i = 0; i < 3; i++)
        {
            // もし頂点が左側なら
            if (isLeftSides[i])
            { // left
                // 左側頂点リストに追加
                CopyTriangle(ref leftTriangle, leftCount, ref triangle, i);
                // 左側頂点カウントを +1
                leftCount++;
            }
            else // それ以外（頂点が右側）なら
            { // right
                // 右側頂点リストに追加
                CopyTriangle(ref rightTriangle, rightCount, ref triangle, i);
                // 右側頂点カウントを +1
                rightCount++;
            }
        }

        // 頂点の割合が1:2（左側が１）の時
        // 左側の頂点を始点としたトライアングルを生成
        if (leftCount == 1)
        {
            CopyTriangle(ref tmpTriangle, 0, ref leftTriangle);
            CopyTriangle(ref tmpTriangle, 1, ref rightTriangle, 0);
            CopyTriangle(ref tmpTriangle, 2, ref rightTriangle, 1);
        }
        else  
        {
            // 頂点の割合が2:1（左側が2）の時
            // 右側の頂点を始点としたトライアングルを生成
            CopyTriangle(ref tmpTriangle, 0, ref rightTriangle);
            CopyTriangle(ref tmpTriangle, 1, ref leftTriangle, 0);
            CopyTriangle(ref tmpTriangle, 2, ref leftTriangle, 1);
        }

        // 1つ側の頂点から、残り2点までの間にあるブレードの交点を求める
        float distance = 0;
        float normalizedDistance = 0.0f;
        Vector3 edgeVector = Vector3.zero;

        // 頂点０から頂点1までのベクトルをedgeVectorへ
        edgeVector = tmpTriangle.vertices[1] - tmpTriangle.vertices[0];
        // Raycast(頂点0からedgeVector（単位ベクトルへ変換）に向けて)レイを照射、距離を取得
        blade.Raycast(new Ray(tmpTriangle.vertices[0], edgeVector.normalized), out distance);

        // edgeVector.magunitude = ベクトルの距離
        // normalizedDistance = ベクトルの距離の比（頂点0から頂点１までの距離を1とした時のブレードまでの距離）
        normalizedDistance = distance / edgeVector.magnitude;
        // Lerp（点１,点2,割合）で、頂点0から頂点１までのベクトルをnormalizedDistanceの割合で作成する
        newTriangle.vertices[0] = Vector3.Lerp(tmpTriangle.vertices[0], tmpTriangle.vertices[1], normalizedDistance);
        // ↑のuvバージョン
        newTriangle.uvs[0] = Vector2.Lerp(tmpTriangle.uvs[0], tmpTriangle.uvs[1], normalizedDistance);
        // ↑の法線ベクトルバージョン
        newTriangle.normals[0] = Vector3.Lerp(tmpTriangle.normals[0], tmpTriangle.normals[1], normalizedDistance);
        // ↑の接線ベクトルバージョン
        newTriangle.tangents[0] = Vector4.Lerp(tmpTriangle.tangents[0], tmpTriangle.tangents[1], normalizedDistance);

        // さっきの処理を頂点0と頂点2で行う
        edgeVector = tmpTriangle.vertices[2] - tmpTriangle.vertices[0];
        blade.Raycast(new Ray(tmpTriangle.vertices[0], edgeVector.normalized), out distance);

        normalizedDistance = distance / edgeVector.magnitude;
        newTriangle.vertices[1] = Vector3.Lerp(tmpTriangle.vertices[0], tmpTriangle.vertices[2], normalizedDistance);
        newTriangle.uvs[1] = Vector2.Lerp(tmpTriangle.uvs[0], tmpTriangle.uvs[2], normalizedDistance);
        newTriangle.normals[1] = Vector3.Lerp(tmpTriangle.normals[0], tmpTriangle.normals[2], normalizedDistance);
        newTriangle.tangents[1] = Vector4.Lerp(tmpTriangle.tangents[0], tmpTriangle.tangents[2], normalizedDistance);

        // 新しく作った頂点がお互い異なるなら成功
        if (newTriangle.vertices[0] != newTriangle.vertices[1])
        {
            //新しい頂点を新ポリゴンリストに追加
            newVertices.Add(newTriangle.vertices[0]);
            newVertices.Add(newTriangle.vertices[1]);
        }
        // 頂点の割合が1:2（左側が１）の時
        if (leftCount == 1)
        {
            // ブレードの左部分ポリゴン
            CopyTriangle(ref tmpTriangle, 0, ref leftTriangle);
            CopyTriangle(ref tmpTriangle, 1, ref newTriangle, 0);
            CopyTriangle(ref tmpTriangle, 2, ref newTriangle, 1);
            // ポリゴンの表面が裏を向いていないかチェック
            NormalCheck(ref tmpTriangle);
            //ポリゴンをリストに追加
            leftMeshMaker.AddTriangle(tmpTriangle, submesh);

            // 右側の4角形を3角形のポリゴン2つにする。
            //右側ポリゴン1つ目
            CopyTriangle(ref tmpTriangle, 0, ref rightTriangle);
            CopyTriangle(ref tmpTriangle, 1, ref newTriangle, 0);
            CopyTriangle(ref tmpTriangle, 2, ref newTriangle, 1);
            NormalCheck(ref tmpTriangle);
            rightMeshMaker.AddTriangle(tmpTriangle, submesh);
            // 右側2つ目
            CopyTriangle(ref tmpTriangle, 0, ref rightTriangle);
            CopyTriangle(ref tmpTriangle, 1, ref rightTriangle);
            CopyTriangle(ref tmpTriangle, 2, ref newTriangle, 1);
            NormalCheck(ref tmpTriangle);
            rightMeshMaker.AddTriangle(tmpTriangle, submesh);
        }
        else //上の処理の左右逆パターン
        {
            CopyTriangle(ref tmpTriangle, 0, ref rightTriangle);
            CopyTriangle(ref tmpTriangle, 1, ref newTriangle, 0);
            CopyTriangle(ref tmpTriangle, 2, ref newTriangle, 1);
            NormalCheck(ref tmpTriangle);
            rightMeshMaker.AddTriangle(tmpTriangle, submesh);

            CopyTriangle(ref tmpTriangle, 0, ref leftTriangle);
            CopyTriangle(ref tmpTriangle, 1, ref newTriangle, 0);
            CopyTriangle(ref tmpTriangle, 2, ref newTriangle, 1);
            NormalCheck(ref tmpTriangle);
            leftMeshMaker.AddTriangle(tmpTriangle, submesh);

            CopyTriangle(ref tmpTriangle, 0, ref leftTriangle);
            CopyTriangle(ref tmpTriangle, 1, ref leftTriangle);
            CopyTriangle(ref tmpTriangle, 2, ref newTriangle, 1);
            NormalCheck(ref tmpTriangle);
            leftMeshMaker.AddTriangle(tmpTriangle, submesh);
        }

    }

    static void CopyTriangle(ref MeshMaker.Triangle tmpTriangle,int i, ref MeshMaker.Triangle dataTriangle)
    {
        CopyTriangle(ref tmpTriangle, i, ref dataTriangle, i);
    }

    static void CopyTriangle(ref MeshMaker.Triangle tmpTriangle, int i, ref MeshMaker.Triangle dataTriangle,int j)
    {
        tmpTriangle.vertices[i] = dataTriangle.vertices[j];
        tmpTriangle.uvs[i] = dataTriangle.uvs[j];
        tmpTriangle.normals[i] = dataTriangle.normals[j];
        tmpTriangle.tangents[i] = dataTriangle.tangents[j];
    }
    #endregion

    #region Capping
    // 断面にメッシュを作る
    // 調べ終わったインデックスを保存するリスト
    private static List<int> usedIndices = new List<int>();
    // 被せるポリゴンのインデックスを保存するリスト
    private static List<int> capPolygonIndices = new List<int>();
    // Functions
    private static void Cap_the_Cut()
    {

        usedIndices.Clear();
        capPolygonIndices.Clear();

        // 必要なポリゴンを見つける
        // 断面に2つの角を作るための頂点を追加する（？）
        // もし2点の座標が同じなら、それらは結合している

        // 新しく生成された頂点の数繰り返す
        // += 2なのは、頂点は2つずつ生成されているため
        for (int i = 0; i < newVertices.Count; i += 2)
        {
            // iが調査済みの場合スキップ
            if (usedIndices.Contains(i)) continue; 

            capPolygonIndices.Clear();
            // i,i+1をインデックスとしてリストに追加
            capPolygonIndices.Add(i);
            capPolygonIndices.Add(i + 1);

            // リストに追加したので、調査済みのリストに入れる
            usedIndices.Add(i);
            usedIndices.Add(i + 1);

            // 頂点座標を変数に代入
            Vector3 connectionPointLeft = newVertices[i];
            Vector3 connectionPointRight = newVertices[i + 1];
            // 完了していたらtrue
            bool isDone = false;

            // 重複している頂点がなくなるまで
            while (!isDone)
            {
                isDone = true;

                // 新しく生成された頂点全てに対して実行 += 2
                for (int index = i + 2; index < newVertices.Count; index += 2)
                {
                    // indexが調査済みリストに入っている場合スキップ
                    if (usedIndices.Contains(index)) continue;

                    Vector3 nextPoint1 = newVertices[index];
                    Vector3 nextPoint2 = newVertices[index + 1];

                    //connectionPointLeft,Right が　nextPoint1,2と重複していたら
                    if (connectionPointLeft == nextPoint1 ||
                        connectionPointLeft == nextPoint2 ||
                        connectionPointRight == nextPoint1 ||
                        connectionPointRight == nextPoint2)
                    {
                        // index,+1をusedの配列に代入する
                        usedIndices.Add(index);
                        usedIndices.Add(index + 1);

                        // 重複していたら
                        if (connectionPointLeft == nextPoint1)
                        {
                            // index+1の数字 をポリゴンインデックス配列の先頭に挿入
                            capPolygonIndices.Insert(0, index + 1);
                            // 左接続ポイントにnextPoint2を上書き
                            connectionPointLeft = nextPoint2;
                        }
                        else if (connectionPointLeft == nextPoint2)
                        {   // 重複していたら
                            // indexの数字をポリゴンインデックス配列の先頭に挿入
                            capPolygonIndices.Insert(0, index);
                            // 左接続ポイントにnextPoint1を上書き
                            connectionPointLeft = nextPoint1;
                        }
                        else if (connectionPointRight == nextPoint1)
                        {   // 重複していたら
                            // index+1の数字をポリゴンインデックス配列の先頭に挿入
                            capPolygonIndices.Add(index + 1);
                            // 右接続ポイントにnextPoint2を上書き
                            connectionPointRight = nextPoint2;
                        }
                        else if (connectionPointRight == nextPoint2)
                        {   // 重複していたら
                            capPolygonIndices.Add(index);
                            // 右接続ポイントにnextPoint1を上書き
                            connectionPointRight = nextPoint1;
                        }
                        // 次もループを行う
                        isDone = false;
                    }
                }
            }

            // capPolygonIndecesの最初と最後の座標が同じなら、capPolygonIndecesの最後のインデックスに最初のインデックスを代入する
            if (newVertices[capPolygonIndices[0]] == newVertices[capPolygonIndices[capPolygonIndices.Count - 1]])
            {
                capPolygonIndices[capPolygonIndices.Count - 1] = capPolygonIndices[0];
            }
            else
            {
                // そうでない場合、最初のインデックスを末尾に追加する
                capPolygonIndices.Add(capPolygonIndices[0]); 
            }

            // 1ポリゴンずつふさいでいく
            // ポリゴンのインデックスを渡す
            FillCap(capPolygonIndices);
        }
    }
    private static void FillCap(List<int> indices)
    { 
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

        //ブレードの法線とupwardの外積を求め、横軸を取得する
        Vector3 left = Vector3.Cross(blade.normal, upward);

        Vector3 displacement = Vector3.zero;
        Vector2 newUV1 = Vector2.zero;
        Vector2 newUV2 = Vector2.zero;
        Vector2 newUV3 = Vector2.zero;

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

            // 中心からlink方向へのベクトル
            displacement = link2 - center;
            newUV2 = Vector3.zero;
            newUV2.x = 0.5f + Vector3.Dot(displacement, left);
            newUV2.y = 0.5f + Vector3.Dot(displacement, upward);

            // 中心からlink方向へのベクトル
            displacement = link3 - center;
            newUV3 = Vector3.zero;
            newUV3.x = 0.5f + Vector3.Dot(displacement, left);
            newUV3.y = 0.5f + Vector3.Dot(displacement, upward);

            // 今求めたデータを元に3角形を構成する
            newTriangle.vertices[0] = link1;
            newTriangle.uvs[0] = newUV1;
            newTriangle.normals[0] = -blade.normal;
            newTriangle.tangents[0] = Vector4.zero;

            newTriangle.vertices[1] = link2;
            newTriangle.uvs[1] = newUV2;
            newTriangle.normals[1] = -blade.normal;
            newTriangle.tangents[1] = Vector4.zero;

            newTriangle.vertices[2] = link3;
            newTriangle.uvs[2] = newUV3;
            newTriangle.normals[2] = -blade.normal;
            newTriangle.tangents[2] = Vector4.zero;
            
            // 三角形の向きを調整する
            NormalCheck(ref newTriangle);
            
            leftMeshMaker.AddTriangle(newTriangle, capMatSub);
            
            //同様の位置に反対向きのポリゴンも生成
            newTriangle.normals[0] = blade.normal;
            newTriangle.normals[1] = blade.normal;
            newTriangle.normals[2] = blade.normal;

            NormalCheck(ref newTriangle);

            // 右側のポリゴン群に追加
            rightMeshMaker.AddTriangle(newTriangle, capMatSub);

            // ベクトルの真ん中を削除する
            indices.RemoveAt((iterator + 1) % indices.Count);
            
            // イテレーターを+1（indexのサイズ以下に修正しつつ）
            iterator = (iterator + 1) % indices.Count;
        }

    }
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
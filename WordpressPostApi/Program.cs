using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace WordpressPostApi
{
    class Program
    {
        /// <summary>
        /// post時のステータスを表す列挙型
        /// </summary>
        enum status
        {
            publish,  // 公開
            future,   // 予約済み
            draft,    // 下書き
            pending,  // レビュー待ち
            @private, // 非公開（予約語なので@をつけてます）
            trash     // ゴミ箱
        }

        /// <summary>
        /// メイン処理
        /// </summary>
        static void Main(string[] args)
        {
            // 投稿内容
            PostContent post = new PostContent();
            /**
             * 以下の内容は必ず編集する事
             * ▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼
             */
            // Application Passwordを設定したユーザ名
            const string userName = "username";
            // Application Passwordでﾒﾓした文字列（ログインパスワードではない）
            const string applicationPassword = "xxxx xxxx xxxx xxxx xxxx xxxx";
            // WordpressのURL、最後はスラッシュなし
            const string serverAddress = "http://wordpress.host.hogehoge";
            post.title = "投稿テスト";
            post.excerpt = "抜粋テスト";
            post.content = "本文テスト";
            // categoryとtagはslugでのみ記述できる。
            var category = new List<string>();
            category.Add("category_1");
            var tags = new List<string>();
            tags.Add("tag_1");
            tags.Add("tag_2");
            // 投稿時のステータスを決める
            post.status = status.publish.ToString();
            /**
             * ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
             * ここから下の内容は変更しない
             */

            try
            {
                // Slugからidを探し出す(REST APIではIDでしか登録できない）
                HttpRequest req = new HttpRequest(serverAddress, userName, applicationPassword);
                post.SetCategories(category, req);
                post.SetTags(tags, req);

                // エラーが無ければPost投稿を行う
                var postResult = Regex.Unescape(req.Post("/wp-json/wp/v2/posts", post));
                Console.WriteLine(string.Format("投稿に成功しました:{0}", postResult));
            }
            catch (Exception e)
            {
                // エラー発生時
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            // Enterキー押下を待ち受ける。バッチ実行時は消す。
            Console.WriteLine("処理が終了しました、Enterキーを押してください...");
            Console.ReadLine();
        }

        /// <summary>
        /// postする内容のデータクラス
        /// </summary>
        class PostContent
        {
            public string title;
            public string excerpt;
            public string content;
            public List<int> categories;
            public void SetCategories(List<string> slugs, HttpRequest req)
            {
                const string categoryUrl = "/wp-json/wp/v2/categories?slug={0}";
                List<int> result = new List<int>();
                foreach (string slug in slugs)
                {
                    var res = req.Get(string.Format(categoryUrl, slug));
                    var serializer = new JavaScriptSerializer();
                    // jsonの文字列をCategoriesの配列にキャスト
                    List<Category> response = (List<Category>)serializer.Deserialize(res, typeof(List<Category>));
                    if (response.Count == 0)
                    {
                        throw new Exception(string.Format("カテゴリが存在しません:{0}", slug));
                    }
                    foreach (Category t in response)
                    {
                        result.Add(t.id);
                    }
                }
                categories = result;
            }
            public List<int> tags;
            public void SetTags(List<string> slugs, HttpRequest req)
            {
                const string tagsUrl = "/wp-json/wp/v2/tags?slug={0}";
                List<int> result = new List<int>();
                foreach (string slug in slugs)
                {
                    var res = req.Get(string.Format(tagsUrl, slug));
                    var serializer = new JavaScriptSerializer();
                    List<Tag> response = (List<Tag>)serializer.Deserialize(res, typeof(List<Tag>));
                    if (response.Count == 0)
                    {
                        throw new Exception(string.Format("tagが存在しません:{0}", slug));
                    }
                    foreach (Tag t in response)
                    {
                        result.Add(t.id);
                    }
                }
                tags = result;
            }
            public string status;
        }

        /// <summary>
        /// tagsをjsonにパースするために作ったデータクラス
        /// </summary>
        class Tag
        {
            public int id;
            public string description;
            public string link;
            public string name;
            public string slug;
        }

        /// <summary>
        /// Categoriesをjsonにパースするために作ったデータクラス
        /// </summary>
        class Category
        {
            public int id;
            public string description;
            public string link;
            public string name;
            public string slug;
        }

        /// <summary>
        /// Httpリクエストを送信し、WordpressのAPIを実行するためのクラス。
        /// </summary>

        class HttpRequest
        {

            private HttpClient client;
            private string baseUrl;

            public HttpRequest(string serverAddress, string userName, string applicationPassword)
            {
                client = new HttpClient();
                baseUrl = serverAddress;
                // userName + : + passwordをBase64エンコードした文字列を取得
                string authKey = Convert.ToBase64String(
                    // UTF8にエンコーディングしてバイト配列として取得
                    Encoding.UTF8.GetBytes(string.Format("{0}:{1}", userName, applicationPassword))
                );
                // Application PasswordsはBasic認証を使用するためヘッダを指定する
                client.DefaultRequestHeaders.Authorization = new
                    System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authKey);
            }

            /// <summary>
            /// Getリクエストを実行するメソッド。
            /// </summary>
            public string Get(string url)
            {
                // URLをエスケープ、GETリクエストを行う
                HttpResponseMessage res = client.GetAsync(Uri.EscapeUriString(baseUrl + url)).Result;
                // レスポンスステータスが正常の場合
                if (res.IsSuccessStatusCode)
                {
                    var s = res.Content.ReadAsStringAsync();
                    return s.Result;
                }
                else
                {
                    throw new Exception("Getリクエストに失敗しました。");
                }
            }

            /// <summary>
            /// Postリクエストを実行するメソッド。
            /// </summary>
            public string Post(string url, PostContent data)
            {
                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(data);
                // Content-Type: application/jsonとしてセット
                var content = new StringContent(json, new UTF8Encoding(), "application/json");
                HttpResponseMessage res = client.PostAsync(Uri.EscapeUriString(baseUrl + url), content).Result;
                // レスポンスステータスが正常の場合
                if (res.IsSuccessStatusCode)
                {
                    var s = res.Content.ReadAsStringAsync();
                    return s.Result;
                }
                else
                {
                    throw new Exception(string.Format("投稿に失敗しました。:{0}", json));
                }
            }
        }


    }

}

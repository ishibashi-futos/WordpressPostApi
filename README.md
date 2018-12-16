# WordpressPostApi

WordpressにAPIを使用してPOSTするサンプル

## Require

- Wordpress に Application Passwordsをインストール&設定する必要があります
- WordpressでREST APIが利用可能である必要があります（4.7以降はデフォルト有効）

## Configure

WordpressPostApi/Program.csを開き、以下を設定します(37～52行目)

``` CSharp
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
```

## How to Use

実行するだけ。
実行に成功したら、ボタンを押せば終了。

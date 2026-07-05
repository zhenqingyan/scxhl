# 恒隆面料管理系统 — 部署指南

## 环境要求

- Docker 20.10+
- Docker Compose 2.0+
- MySQL 数据库（阿里云 RDS）

## 快速部署

### 1. 安装 Docker

```bash
curl -fsSL https://get.docker.com | bash
systemctl enable docker && systemctl start docker
```

### 2. 拉取项目

```bash
git clone <仓库地址> /opt/henglong
cd /opt/henglong
```

### 3. 配置环境变量

```bash
cp .env.example .env
vim .env
```

按实际情况填写：

```env
ConnectionStrings__MySql=Server=你的数据库地址;Port=3306;Database=henglong;User=root;Password=你的密码;SslMode=Preferred;CharSet=utf8mb4;
AliyunOss__EndPoint=oss-cn-shanghai.aliyuncs.com
AliyunOss__AccessKey=你的AccessKey
AliyunOss__AccessSecret=你的AccessSecret
AliyunOss__BucketName=henglong
DISABLE_HTTPS=true
```

### 4. 启动

```bash
docker-compose up -d
```

### 5. 验证

```bash
# 健康检查
curl http://localhost:8080/healthz

# 访问首页
curl http://localhost:8080/
```

浏览器访问 `http://<服务器IP>:8080/`。

## 常用命令

```bash
# 查看日志
docker-compose logs -f

# 重启
docker-compose restart

# 停止
docker-compose down

# 重新构建（代码更新后）
docker-compose up -d --build
```

## 更新代码

本地修改推送到 git 后，在服务器上执行：

```bash
cd /opt/henglong
git pull
docker-compose up -d --build
```

`--build` 会重新构建镜像，旧容器自动替换，**`.env` 配置不会丢失**。

## 配置 Nginx 反向代理（可选）

```nginx
server {
    listen 80;
    server_name your-domain.com;

    client_max_body_size 30m;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

> 如需 HTTPS，可使用 `certbot --nginx` 自动配置 Let's Encrypt 证书。

## 配置文件说明

| 文件 | 用途 | 是否提交 git |
|------|------|:---:|
| `Dockerfile` | 镜像构建定义 | ✅ |
| `docker-compose.yml` | 服务编排，引用 `.env` | ✅ |
| `.env.example` | 环境变量模板 | ✅ |
| `.env` | 真实密钥，容器启动时读取 | ❌ |
| `.dockerignore` | Docker 构建时排除的文件 | ✅ |

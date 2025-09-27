Ejabberd guide:

Requirements:
- Domain 
- Certificate
- Postgresql (not needed)

Issues:
- Audio calling not working flawlessly yet


Steps:

Put Certificates in folder (see volumes)
Create ejabberd.yml on location (see volumes)

port forwarding:
5222,5223,5269, 3478, 5349, 49152-49159



Docker compose:

```yaml
services:
  ejabberd:
    image: ejabberd/ecs:latest
    container_name: ejabberd-1
    command: foreground
    restart: always
    network_mode: bridge
    environment:
      PATH: /usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/home/ejabberd/bin
      TERM: xterm
      LC_ALL: C.UTF-8
      LANG: en_US.UTF-8
      LANGUAGE: en_US.UTF-8
      REPLACE_OS_VARS: "true"
      HOME: /home/ejabberd
      VERSION: "25.07"
    ports:
	# client to server traffic
      - "5222:5222"
      - "5223:5223"
	# Server to server traffic
      - "5269:5269"	  
	# Local http traffic for admin screen
	  - "5280:5280"
	# Https traffic for file uploads and javascript clients
	  - "5443:5443"
	  
	# STUN/TURN ports
      - "3478:3478/udp"
      - "5349:5349"
	# Audio/Video ports
      - "49152:49152/udp"
      - "49153:49153/udp"
      - "49154:49154/udp"
      - "49155:49155/udp"
      - "49156:49156/udp"
      - "49157:49157/udp"
      - "49158:49158/udp"
      - "49159:49159/udp"
    volumes:
      - /Media/Config/Ejabberd/certs:/certs:rw
      - /Media/Config/Ejabberd/ejabberd.yml:/opt/ejabberd/conf/ejabberd.yml:rw
      - /Media/Data/Ejabberd:/upload:rw
```	  
	  

Ejabberd.yml:

```yaml
# replace these values:
# <domain>              e.g.: test.com
# <publicip>            e.g.: 123.123.123.123
# <admin>               e.g.: test@test.com
# <email>               e.g.: test@gmail.com
 
# <dbhost>              e.g.: 123.123.123.123
# <dbport>              e.g.: 5123
# <dbdatabase>          e.g.: ejabberd
# <dbuser>              e.g.: ejabberd
# <dbpassword>          e.g.: password

hosts:
  - "<domain>"

listen:
  -
    port: 5222
    ip: "::"
    transport: tcp
    module: ejabberd_c2s
    max_stanza_size: 262144
    shaper: c2s_shaper
    starttls: true
    protocol_options:
     - "no_sslv3"
     - "no_tlsv1"
     - "no_tlsv1_1"
     - "tlsv1_2"
     - "tlsv1_3"
  -
    port: 5223
    ip: "::"
    module: ejabberd_c2s
    max_stanza_size: 262144
    shaper: c2s_shaper
    access: c2s
    tls: true
    protocol_options:
     - "no_sslv3"
     - "no_tlsv1"
     - "no_tlsv1_1"
     - "tlsv1_2"
     - "tlsv1_3"
  -
    port: 5269
    ip: "::"
    transport: tcp
    module: ejabberd_s2s_in
    max_stanza_size: 524288
    shaper: s2s_shaper
  -
    port: 5280
    ip: "::"
    module: ejabberd_http
    tls: false
    request_handlers:
      /admin: ejabberd_web_admin
      /.well-known/acme-challenge: ejabberd_acme
      /register: mod_register_web
  -
    port: 5443
    module: ejabberd_http
    tls: true
    request_handlers:
   #   /api: mod_http_api
   #   /bosh: mod_bosh
   #   /captcha: ejabberd_captcha
      /upload: mod_http_upload
   #  /ws: ejabberd_http_ws
   #  /register: mod_register_web
  -
    port: 3478
    transport: udp
    module: ejabberd_stun
    use_turn: true
    turn_min_port: 49152
    turn_max_port: 49159
    ## The server's public IPv4 address:
    turn_ipv4_address: <publicip>
  -
    port: 5349
    transport: tcp
    module: ejabberd_stun
    use_turn: true
    tls: true
    turn_min_port: 49152
    turn_max_port: 49159
    turn_ipv4_address: <publicip>
#  -
#    port: 1883
#    ip: "::"
#    module: mod_mqtt
#    backlog: 1000
auth_method: internal

s2s_use_starttls: required
s2s_ciphers: "HIGH:!3DES:!aNULL:!SSLv2:@STRENGTH"
s2s_protocol_options:                                                            
  - "no_sslv2"                                                                   
  - "no_sslv3"

acl:
  local:
    user_regexp: ""
  admin:
    user:
      - "<admin>"
  loopback:
    ip:
      - 127.0.0.0/8
      - ::1/128
  server:
    server: <domain>
access_rules:
  local:
    allow: local
  c2s:
    deny: blocked
    allow: all
  announce:
    allow: admin
  configure:
    allow: admin
  muc_create:
    allow: local
  pubsub_createnode:
    allow: local
  trusted_network:
    allow: loopback
  s2s:
    allow: all
  upload:
    allow: server

#api_permissions:
#  "console commands":
#    from: ejabberd_ctl
#    who: all
#    what: "*"
#  "webadmin commands":
#    from: ejabberd_web_admin
#    who: admin
#    what: "*"
#  "adhoc commands":
#    from: mod_adhoc_api
#    who: admin
#    what: "*"
#  "http access":
#    from: mod_http_api
#    who:
#      access:
#        allow:
#          - acl: loopback
#          - acl: admin
#      oauth:
#        scope: "ejabberd:admin"
#        access:
#          allow:
#            - acl: loopback
#            - acl: admin
#    what:
#      - "*"
#      - "!stop"
#      - "!start"
#  "public commands":
#    who:
#      ip: 127.0.0.1/8
#    what:
#      - status
#      - connected_users_number

shaper:
  normal:
    rate: 3000
    burst_size: 20000
  fast: 100000

shaper_rules:
  max_user_sessions: 10
  max_user_offline_messages:
    5000: admin
    100: all
  c2s_shaper:
    none: admin
    normal: all
  s2s_shaper: fast

    
modules:
  mod_adhoc: {}
  mod_adhoc_api: {}
  mod_admin_extra: {}
  mod_announce:
    access: announce
  mod_avatar: {}
  mod_blocking: {}
  mod_bosh: {}
  mod_caps: {}
  mod_carboncopy: {}
  mod_client_state: {}
  mod_configure: {}
  mod_disco: 
    server_info:
    -
        modules: all
        name: "abuse-addresses"
        urls: ["mailto:<email>"]
  mod_fail2ban: {}
  mod_http_api: {}
  mod_http_upload:
    access: upload
    docroot: /upload
    put_url: "https://<domain>:5443/upload"
    custom_headers:
      "Access-Control-Allow-Origin": "https://<domain>"
      "Access-Control-Allow-Methods": "GET,HEAD,PUT,OPTIONS"
      "Access-Control-Allow-Headers": "Content-Type"
  mod_last: {}
  mod_mam:
    ## Mnesia is limited to 2GB, better to use an SQL backend
    ## For small servers SQLite is a good fit and is very easy
    ## to configure. Uncomment this when you have SQL configured:
    db_type: sql
    assume_mam_usage: true
    default: always
  mod_mqtt: {}
  mod_muc:
    access:
      - allow
    access_admin:
      - allow: admin
    access_create: muc_create
    access_persistent: muc_create
    access_mam:
      - allow
    default_room_options:
      mam: true
  mod_muc_admin: {}
  mod_muc_occupantid: {}
  mod_offline:
    access_max_user_messages: max_user_offline_messages
  mod_ping: {}
  mod_privacy: {}
  mod_private: {}
  mod_proxy65:
    access: local
    max_connections: 5
  mod_pubsub:
    access_createnode: pubsub_createnode
    plugins:
      - flat
      - pep
    force_node_config:
      ## Avoid buggy clients to make their bookmarks public
      storage:bookmarks:
        access_model: whitelist
  mod_push: {}
  mod_push_keepalive: {}
 # mod_register:
 #   registration_watchers: [<admin>]
 #   captcha_protected: true
 #   ## Only accept registration requests from the "trusted"
 #   ## network (see access_rules section above).
 #   ## Think twice before enabling registration from any
 #   ## address. See the Jabber SPAM Manifesto for details:
 #   ## https://github.com/ge0rg/jabber-spam-fighting-manifesto
 #   ip_access: all
  mod_roster:
    versioning: true
  mod_s2s_bidi: {}
  mod_s2s_dialback: {}
  mod_shared_roster: {}
  mod_stream_mgmt: {}
  mod_stun_disco:
    credentials_lifetime: 12h
    services:
        -
          host: <publicip>
          port: 3478
          type: stun
          transport: udp
          restricted: false
        -
          host: <publicip>
          port: 3478
          type: turn
          transport: udp
          restricted: true
        -
          host: <domain>
          port: 5349
          type: stuns
          transport: tcp
          restricted: false
        -
          host: <domain>
          port: 5349
          type: turns
          transport: tcp
          restricted: true
  mod_vcard: {}
  mod_vcard_xupdate: {}
  mod_version:
    show_os: false
host_config:
  <domain>:
    sql_type: pgsql
    sql_server: <dbhost>
    sql_port: <dbport>
    sql_database: <dbdatabase>
    sql_username: <dbuser>
    sql_password: <dbpassword>
    auth_method: [sql]

sql_type: pgsql
sql_server: <dbhost>
sql_port: <dbport>
sql_database: <dbdatabase>
sql_username: <dbuser>
sql_password: <dbpassword>
default_db: sql

loglevel: 4
log_rotate_size: 10485760
log_rotate_date: ""
log_rotate_count: 1
log_rate_limit: 100

captcha_cmd: "$HOME/bin/captcha.sh"
captcha_url: https://<domain>:5443/captcha
certfiles:
  - "/certs/*.pem"
```

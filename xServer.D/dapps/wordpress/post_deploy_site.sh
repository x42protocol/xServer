#!/bin/bash

if [ $# -lt 5 ]; then
  echo "Usage: $0 appname domain email my.wp.db.password my.root.db.password"
  echo "Usage: $0 wordpress mywordpress.wordpresspreview.site my@email.com mysecetwppass mysupersecretrootpass"
  exit 1
fi

APP_NAME=$1
DOMAIN=$2
DOMAIN_LOWER=$(echo "$DOMAIN" | tr '[:upper:]' '[:lower:]' | sed 's/\./'_'/g')
DOMAIN_NODOT=$(echo "$DOMAIN_LOWER" | sed 's/\_//g')
EMAIL=$3
MYSQL_PASSWORD=$4
MYSQL_ROOT_PASSWORD=$5

main(){

	cd ${DOMAIN}
	

	
	docker container create --name  temp_container1 -v ${DOMAIN_NODOT}_container:/usr/local/bin busybox
	docker cp ./bin/container/. temp_container1:/usr/local/bin
	docker rm temp_container1
	
	docker run --rm -dit -v ${DOMAIN_NODOT}_sites:/var/www/vhosts/${DOMAIN}/ alpine ash -c "mkdir /var/www/vhosts/${DOMAIN}/${DOMAIN}"
	docker run --rm -dit -v ${DOMAIN_NODOT}_sites:/var/www/vhosts/${DOMAIN}/ alpine ash -c "mkdir /var/www/vhosts/${DOMAIN}/${DOMAIN}/html"
	docker run --rm -dit -v ${DOMAIN_NODOT}_sites:/var/www/vhosts/${DOMAIN}/ alpine ash -c "mkdir /var/www/vhosts/${DOMAIN}/${DOMAIN}/logs"
	docker run --rm -dit -v ${DOMAIN_NODOT}_sites:/var/www/vhosts/${DOMAIN}/ alpine ash -c "mkdir /var/www/vhosts/${DOMAIN}/${DOMAIN}/certs"
	

	echo "Adding Domain ${DOMAIN}"
	source ./bin/domain.sh -A ${DOMAIN}
	
	echo "Adding Database"
	bash ./bin/database.sh -D ${DOMAIN}

	
	docker container create --name  temp_container2 -v ${DOMAIN_NODOT}_sites:/var/www/vhosts busybox
	docker cp sites/${DOMAIN}/.db_pass temp_container2:/var/www/vhosts/${DOMAIN}/.db_pass
	docker rm temp_container2

	echo "Installing ${APP_NAME} on ${DOMAIN}"
	bash ./bin/appinstall.sh -A ${APP_NAME} -D ${DOMAIN}
	
	echo "Done."
}

main

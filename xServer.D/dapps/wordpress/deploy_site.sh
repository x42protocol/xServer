#!/bin/bash

if [ $# -lt 5 ]; then
  echo "Usage: $0 appname domain email my.wp.db.password my.root.db.password"
  echo "Usage: $0 wordpress mywordpress.wordpresspreview.site my@email.com mysecetwppass mysupersecretrootpass"
  exit 1
fi

APP_NAME=$1
DOMAIN=$2
DOMAIN_LOWER=$(echo "$DOMAIN" | tr '[:upper:]' '[:lower:]' | sed 's/\./'_'/g')
EMAIL=$3
MYSQL_PASSWORD=$4
MYSQL_ROOT_PASSWORD=$5

main(){
	echo "Setting Up ${DOMAIN_LOWER}"
	echo Setting up Envrionment
	mkdir -p sites/${DOMAIN}
	sed -e 's/#DOMAIN#/'${DOMAIN}'/g' -e 's/#domain#/'${DOMAIN_LOWER}'/g' docker-compose.yml > sites/${DOMAIN}/docker-compose.yml
	cp -r bin sites/${DOMAIN}/
	cd sites/${DOMAIN}
	mkdir acme
	mkdir data
	mkdir logs
	mkdir lsws
	mkdir sites

cat <<EOF > .env
TimeZone=America/New_York
OLS_VERSION=1.7.15
PHP_VERSION=lsphp80
MYSQL_DATABASE=wordpress
MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD}
MYSQL_USER=wordpress
MYSQL_PASSWORD=${MYSQL_PASSWORD}
DOMAIN=${DOMAIN}
EOF

	docker-compose up -d
	
	echo "Adding Domain ${DOMAIN}"
	source ./bin/domain.sh -A ${DOMAIN}
	
	echo "Adding Database"
	bash ./bin/database.sh -D ${DOMAIN}
	echo "Installing ${APP_NAME} on ${DOMAIN}"
	bash ./bin/appinstall.sh -A ${APP_NAME} -D ${DOMAIN}
	
#	echo "Issuing Certificate on ${DOMAIN}"
#	source ./bin/acme.sh -I -E ${EMAIL} -D ${DOMAIN}
	
	echo "Done."
}

main

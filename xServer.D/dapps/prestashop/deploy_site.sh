#!/bin/bash

if [ $# -lt 5 ]; then
  echo "Usage: $0 appname domain email my.wp.db.password my.root.db.password"
  echo "Usage: $0 prestashop prestashop.x42.site my@email.com mysecetwppass mysupersecretrootpass"
  exit 1
fi

APP_NAME=$1
DOMAIN=$2
DOMAIN_LOWER=$(echo "$DOMAIN" | tr '[:upper:]' '[:lower:]' )
DOMAIN_LOWER_USCORE=$(echo "$DOMAIN" | tr '[:upper:]' '[:lower:]' | sed 's/\./'_'/g')
DOMAIN_STRIPPEDLOWER=$(echo "$DOMAIN_LOWER_USCORE" | sed 's/_//g')
EMAIL=$3
MYSQL_PASSWORD=$4
MYSQL_ROOT_PASSWORD=$5

main(){
	echo "Setting Up ${DOMAIN_LOWER_USCORE}"
	echo Setting up Envrionment
	mkdir -p sites/${DOMAIN}
	sed -e 's/#DOMAIN#/'${DOMAIN}'/g' -e 's/#domain#/'${DOMAIN_LOWER_USCORE}'/g' docker-compose.yml > sites/${DOMAIN}/docker-compose.yml
	cp -r bin sites/${DOMAIN}/
	chmod -R +x sites/${DOMAIN}/bin
	cd sites/${DOMAIN}
	mkdir acme
	mkdir data
	mkdir logs
	mkdir lsws
	mkdir sites
	chmod 755 lsws

cat <<EOF > .env
TimeZone=America/New_York
LSWS_VERSION=5.4.12
PHP_VERSION=lsphp73
MYSQL_DATABASE=prestashop
MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD}
MYSQL_USER=prestashop
MYSQL_PASSWORD=${MYSQL_PASSWORD}
DOMAIN=localhost
EOF

	docker-compose up -d
	
	echo "Adding Domain ${DOMAIN}"
	source ./bin/domain.sh -A ${DOMAIN}
	
	echo "Adding Database"
	bash ./bin/database.sh -D ${DOMAIN}

	echo "Installing ${APP_NAME} on ${DOMAIN}"
	bash ./bin/appinstall.sh -A ${APP_NAME} -D ${DOMAIN} 
	
	docker exec ${DOMAIN_STRIPPEDLOWER}_mysql_1 mysql -uroot -p${MYSQL_ROOT_PASSWORD}  <<< "use prestashopxservernetwork; update ps_configuration set value=1 where name like 'PS_SSL_ENABLED%';"
	
	echo "Done."
}

main

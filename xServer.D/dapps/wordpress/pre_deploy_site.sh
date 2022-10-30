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
	
	cd /app/dapps/wordpress

	mkdir -p sites/${DOMAIN}
	
	sed -e 's/#DOMAIN#/'${DOMAIN}'/g' -e 's/#domain#/'${DOMAIN_LOWER}'/g' docker-compose.yml > ./sites/${DOMAIN}/docker-compose.yml
	
	cp -r bin sites/${DOMAIN}/
	cd sites/${DOMAIN}

	mkdir acme
	mkdir data
	mkdir logs
	mkdir lsws
	mkdir sites

}

main
